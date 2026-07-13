using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Battle;
using FinalProject.Core;
using FinalProject.Core.StateMachine;
using FinalProject.Core.Text;
using FinalProject.Data;

namespace FinalProject.States
{
    // one boss fight against a Teacher. Pushed onto the state stack when an
    // encounter starts, pops itself when the fight ends.
    public class BattleState : GameState
    {
        private readonly Teacher _teacher;
        private readonly Random  _rng = new();

        private BattlePhase _phase;
        private BattlePhase _phaseAfterTransition;
        private PlayerMoveList? _playerChoice;
        private DodgePhase  _dodgePhase;
        private BattleMenu  _menu;
        private ActionMenu  _actionMenu;
        private TweeningBox _box;

        // FIGHT's timing bar, its result is the damage UpdateTurnExecution applies
        private AttackMinigame _attackMinigame;
        private int _pendingAttackDamage;

        // only reset when the text actually changes, otherwise backing out of
        // a submenu would restart the typing animation
        private readonly Typewriter _narrationTypewriter = new();
        private string _lastNarrationText;

        private const float ShrinkSeconds = 0.6f;
        private const float GrowSeconds   = 0.6f;
        private const float NarrationTextScale = 2f;

        // wide box for menus, square box for dodging. The wide box is sized
        // so the inside (minus the 2px border) is exactly 546x114 - the
        // attack minigame art at native size, no scaling
        private static readonly Rectangle WideBoxRect = new Rectangle(
            (GameSettings.WindowWidth - 550) / 2, 228, 550, 118);
        private static readonly Rectangle DodgeBoxRect = new Rectangle(
            (GameSettings.WindowWidth - GameSettings.DodgeBoxDefaultSize) / 2, 185,
            GameSettings.DodgeBoxDefaultSize, GameSettings.DodgeBoxDefaultSize);

        public BattleState(Game1 game, GameStateManager sm, Teacher teacher)
            : base(game, sm)
        {
            _teacher = teacher;
        }

        public override void OnEnter()
        {
            _phase        = BattlePhase.SelectingMove;
            _playerChoice = null;
            _dodgePhase   = null;
            _menu         = new BattleMenu();
            _actionMenu   = new ActionMenu();
            _box          = new TweeningBox(WideBoxRect);
            _lastNarrationText   = null;
            _attackMinigame      = null;
            _pendingAttackDamage = 0;
        }

        public override void Update(GameTime gameTime)
        {
            switch (_phase)
            {
                case BattlePhase.SelectingMove:
                    UpdateMoveSelection(gameTime);
                    break;

                case BattlePhase.ActionMenu:
                    UpdateActionMenu(gameTime);
                    break;

                case BattlePhase.AttackMinigame:
                    UpdateAttackMinigame(gameTime);
                    break;

                case BattlePhase.ExecutingTurn:
                    UpdateTurnExecution();
                    break;

                case BattlePhase.BoxTransition:
                    UpdateBoxTransition(gameTime);
                    break;

                case BattlePhase.Dodging:
                    UpdateDodging(gameTime);
                    break;

                case BattlePhase.BattleOver:
                    StateManager.Pop();
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            DrawHpStatus(spriteBatch);

            if (_phase == BattlePhase.Dodging)
            {
                _dodgePhase.Draw(spriteBatch, Game.PixelTexture);
            }
            else
            {
                DrawBoxBorder(spriteBatch, _box.Current);

                if (_phase == BattlePhase.ActionMenu)
                    _actionMenu.Draw(spriteBatch, Game.DialogueFont, _box.Current);

                if (_phase == BattlePhase.AttackMinigame)
                    _attackMinigame.Draw(spriteBatch);

                // narration shows while picking an action, the submenus replace it
                if (_phase == BattlePhase.SelectingMove)
                    DrawTeacherNarration(spriteBatch);

                // buttons stay on screen the whole time (they only disappear
                // while dodging), the soul only sits on them while choosing
                _menu.Draw(spriteBatch, showSoul: _phase == BattlePhase.SelectingMove);
            }

            spriteBatch.End();
        }

        // ── Phase: SelectingMove ─────────────────────────────────────────────

        private void UpdateMoveSelection(GameTime gameTime)
        {
            RefreshNarration();
            _narrationTypewriter.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            if (_menu.Update(Game.Input, out PlayerMoveList choice))
            {
                _actionMenu.Open(BuildRootPage(choice));
                _phase = BattlePhase.ActionMenu;
            }
        }

        // Rebuilds the narration typewriter for the teacher's current HP band,
        // but only when the text actually changed — so backing out of a submenu
        // doesn't restart the typing. To force a re-type (e.g. after an enemy
        // turn), clear _lastNarrationText first.
        private void RefreshNarration()
        {
            // prefer the spare-based narration once mercy progress has overtaken
            // the teacher's remaining HP%; otherwise use the HP-based narration
            float hp      = CurrentHpPercent();
            float spare   = _teacher.SparePercent;
            var   bySpare = _teacher.Stats.TurnNarrationBySpare;

            string narrationText = (bySpare != null && bySpare.Count > 0 && spare > hp)
                ? PercentThresholdText.Resolve(bySpare, spare)
                : PercentThresholdText.Resolve(_teacher.Stats.TurnNarration, hp);
            if (narrationText == _lastNarrationText) return;

            // bake the wrap in up front so the line breaks can't shift
            // while the text is still typing out
            float maxWidth = WideBoxRect.Width - 32;
            _narrationTypewriter.SetText(string.Join("\n",
                TextWrap.ToLines(Game.DialogueFont, narrationText, maxWidth, NarrationTextScale)));
            _lastNarrationText = narrationText;
        }

        // ── Phase: ActionMenu ────────────────────────────────────────────────

        private void UpdateActionMenu(GameTime gameTime)
        {
            if (!_actionMenu.Update(gameTime, Game.Input))
                _phase = BattlePhase.SelectingMove;
        }

        private List<MenuOption> BuildRootPage(PlayerMoveList choice) => choice switch
        {
            PlayerMoveList.Attack => BuildFightRootPage(),
            PlayerMoveList.Act    => BuildActRootPage(),
            PlayerMoveList.Item   => BuildItemRootPage(),
            PlayerMoveList.Spare  => BuildSpareRootPage(),
            _                     => BuildFightRootPage()
        };

        // confirming a target starts the timing bar, damage gets decided there
        private List<MenuOption> BuildFightRootPage() => new()
        {
            new MenuOption(_teacher.Name, () =>
            {
                _attackMinigame = new AttackMinigame(BoxInterior(), _rng);
                _phase = BattlePhase.AttackMinigame;
            })
        };

        private List<MenuOption> BuildActRootPage() => new()
        {
            new MenuOption(_teacher.Name, () => _actionMenu.Push(BuildActOptionsPage()))
        };

        private List<MenuOption> BuildActOptionsPage()
        {
            var page = new List<MenuOption>();
            foreach (ActOption opt in _teacher.Stats.ActOptions)
                page.Add(new MenuOption(opt.Name, () =>
                {
                    _teacher.IncreaseSparePercent(opt.SpareGain);
                    PushMessageSequence(opt.GetMessages(CurrentHpPercent()), PlayerMoveList.Act);
                }));
            return page;
        }

        // opening ITEM with no items doesn't waste the turn, it just backs
        // out (same as pressing X). Only actually using an item costs a turn
        private List<MenuOption> BuildItemRootPage()
        {
            if (Game.PlayerData.Inventory.Count == 0)
            {
                return new List<MenuOption>
                {
                    new MenuOption("You have no items.", () => _phase = BattlePhase.SelectingMove)
                };
            }

            var page = new List<MenuOption>();
            foreach (ItemData item in Game.PlayerData.Inventory)
                page.Add(new MenuOption(item.Name, () => UseItem(item)));
            return page;
        }

        private void UseItem(ItemData item)
        {
            Game.PlayerData.Heal(item.HealAmount);
            Game.PlayerData.Inventory.Remove(item);
            PushMessageSequence(
                new List<string> { $"You ate the {item.Name}. Restored {item.HealAmount} HP." },
                PlayerMoveList.Item);
        }

        private List<MenuOption> BuildSpareRootPage() => new()
        {
            new MenuOption(_teacher.Name, () => _actionMenu.Push(BuildSpareConfirmPage()))
        };

        private List<MenuOption> BuildSpareConfirmPage() => new()
        {
            new MenuOption("Spare", () =>
            {
                if (_teacher.SparePercent >= _teacher.Stats.SpareSuccessAt)
                {
                    _teacher.Spare();
                    PushMessageSequence(new List<string> { _teacher.Stats.SpareSuccessText }, PlayerMoveList.Spare);
                }
                else
                {
                    string text = PercentThresholdText.Resolve(_teacher.Stats.SpareTextByPercent, _teacher.SparePercent);
                    PushMessageSequence(new List<string> { text }, PlayerMoveList.Spare);
                }
            })
        };

        // shows messages one after another, Z on the last one ends the turn
        private void PushMessageSequence(List<string> messages, PlayerMoveList finalChoice, int index = 0)
        {
            bool isLast = index >= messages.Count - 1;
            _actionMenu.Push(new List<MenuOption>
            {
                new MenuOption(messages[index], isLast
                    ? () => FinalizeChoice(finalChoice)
                    : () => PushMessageSequence(messages, finalChoice, index + 1))
            }, allowCancel: false, typewriter: true);
        }

        private void FinalizeChoice(PlayerMoveList choice)
        {
            _playerChoice = choice;
            _phase = BattlePhase.ExecutingTurn;
        }

        // ── Phase: AttackMinigame ────────────────────────────────────────────

        private void UpdateAttackMinigame(GameTime gameTime)
        {
            _attackMinigame.Update(gameTime, Game.Input);

            if (!_attackMinigame.IsFinished) return;

            // a miss still spends the turn, it just deals nothing. A hit
            // scales attack by accuracy (min 1 so edge hits still count)
            _pendingAttackDamage = _attackMinigame.Missed
                ? 0
                : Math.Max(1, (int)MathF.Round(Game.PlayerData.Attack * _attackMinigame.DamageMultiplier));

            _attackMinigame = null;
            FinalizeChoice(PlayerMoveList.Attack);
        }

        // the box minus its 2px border
        private Rectangle BoxInterior()
        {
            Rectangle r = _box.Current;
            return new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
        }

        // ── Phase: ExecutingTurn ─────────────────────────────────────────────

        private void UpdateTurnExecution()
        {
            if (_playerChoice == PlayerMoveList.Attack)
                _teacher.TakeDamage(_pendingAttackDamage);
            // act/spare/item already did their thing in their Activate callbacks

            _playerChoice = null;

            if (IsBattleOver())
            {
                _phase = BattlePhase.BattleOver;
                return;
            }

            MoveData move = _teacher.Moves[_rng.Next(_teacher.Moves.Count)];
            IBulletPattern pattern = move.CreatePattern();
            _dodgePhase = new DodgePhase(pattern, _teacher, Game.PlayerData, DodgeBoxRect);

            _box.ResizeTo(DodgeBoxRect, ShrinkSeconds);
            _phaseAfterTransition = BattlePhase.Dodging;
            _phase = BattlePhase.BoxTransition;
        }

        // ── Phase: BoxTransition ─────────────────────────────────────────────

        private void UpdateBoxTransition(GameTime gameTime)
        {
            _box.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            if (!_box.IsAnimating)
                _phase = _phaseAfterTransition;
        }

        // ── Phase: Dodging ───────────────────────────────────────────────────

        private void UpdateDodging(GameTime gameTime)
        {
            _dodgePhase.Update(gameTime, Game.Input);

            // stop early if the player died mid-dodge
            if (IsBattleOver())
            {
                _phase = BattlePhase.BattleOver;
                return;
            }

            if (_dodgePhase.IsFinished)
            {
                _dodgePhase = null;
                _box.ResizeTo(WideBoxRect, GrowSeconds);
                // a finished enemy turn is a fresh turn — force the narration to
                // re-type. Prime it to zero now (during the box transition, before
                // the first SelectingMove draw) so there's no one-frame flash of
                // the fully-typed text from last turn. Unlike backing out of a
                // submenu, which keeps the guard and shows the text instantly.
                _lastNarrationText = null;
                RefreshNarration();
                _phaseAfterTransition = BattlePhase.SelectingMove;
                _phase = BattlePhase.BoxTransition;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool IsBattleOver()
            => !Game.PlayerData.IsAlive || !_teacher.IsAlive || _teacher.IsSpared;

        private float CurrentHpPercent()
            => _teacher.MaxHp > 0 ? (float)_teacher.CurrentHp / _teacher.MaxHp * 100f : 0f;

        // the flavor text about what the teacher is about to do. Plain text,
        // no soul cursor since it's not selectable. The text was pre-wrapped
        // when it was set (SpriteFont draws embedded newlines natively)
        private void DrawTeacherNarration(SpriteBatch spriteBatch)
        {
            Rectangle box = _box.Current;
            spriteBatch.DrawString(Game.DialogueFont, _narrationTypewriter.VisibleText,
                new Vector2(box.X + 16, box.Y + 16), Color.White,
                0f, Vector2.Zero, NarrationTextScale, SpriteEffects.None, 0f);
        }

        private void DrawBoxBorder(SpriteBatch spriteBatch, Rectangle rect)
        {
            const int borderThickness = 2;
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, borderThickness), Color.White);
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.X, rect.Bottom - borderThickness, rect.Width, borderThickness), Color.White);
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.X, rect.Y, borderThickness, rect.Height), Color.White);
            spriteBatch.Draw(Game.PixelTexture, new Rectangle(rect.Right - borderThickness, rect.Y, borderThickness, rect.Height), Color.White);
        }
        
        // drawn in every phase so HP loss is always visible
        private void DrawHpStatus(SpriteBatch spriteBatch)
        {
            const int x = 40;
            const int   y = 390;
            const float textScale = 2f;

            spriteBatch.DrawString(Game.DialogueFont, "HP", new Vector2(x, y), Color.White,
                0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

            var barBg = new Rectangle(x + 60, y + 4, 100, 16);
            spriteBatch.Draw(Game.PixelTexture, barBg, new Color(60, 20, 20));

            float hpPercent = Game.PlayerData.MaxHp > 0
                ? MathHelper.Clamp((float)Game.PlayerData.CurrentHp / Game.PlayerData.MaxHp, 0f, 1f)
                : 0f;
            var fill = new Rectangle(barBg.X, barBg.Y, (int)(barBg.Width * hpPercent), barBg.Height);
            spriteBatch.Draw(Game.PixelTexture, fill, Color.Yellow);

            string hpText = $"{Game.PlayerData.CurrentHp} / {Game.PlayerData.MaxHp}";
            spriteBatch.DrawString(Game.DialogueFont, hpText, new Vector2(barBg.Right + 16, y), Color.White,
                0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }
    }

    public enum PlayerMoveList { Attack, Act, Item, Spare }

    public enum BattlePhase { SelectingMove, ActionMenu, AttackMinigame, ExecutingTurn, BoxTransition, Dodging, BattleOver }
}
