using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Battle;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.StateMachine;
using FinalProject.Core.Text;
using FinalProject.Data;
using FinalProject.UI;

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

        // the HP bar. it handles its own EventBus subscription (see BattleHud),
        // this state just makes it, draws it and unhooks it when the fight ends
        private BattleHud _hud;

        // the teacher's multi part sprite, built from its JSON. null-safe, if the
        // teacher has no sprite block it just draws nothing
        private TeacherSprite _teacherSprite;

        // centred over the battle box and sitting just above it, undertale style.
        // worked out from his assembled size so it stays right at any scale.
        // the gap is small on purpose, it leaves room above his head for the
        // HP bar that shows when he's hit
        private Vector2 TeacherAnchor => new Vector2(
            WideBoxRect.Center.X - _teacherSprite.Size.X / 2f,
            WideBoxRect.Y - _teacherSprite.Size.Y - 2f);

        // his rect on screen, the damage display hangs the slash/number/bar off it
        private Rectangle TeacherBounds => new Rectangle(
            (int)TeacherAnchor.X, (int)TeacherAnchor.Y,
            (int)_teacherSprite.Size.X, (int)_teacherSprite.Size.Y);

        // slash + damage number + the teacher's HP bar, shown right after a hit
        private readonly DamageDisplay _damageDisplay = new();

        // set when an attack lands, spent once the slash finishes
        private bool _hurtQueued;

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

            // The HUD subscribes to the EventBus itself; hand it the current HP
            // to seed the display (the event only fires on a later change).
            _hud = new BattleHud(Game.PlayerData.CurrentHp, Game.PlayerData.MaxHp);
            _teacherSprite = new TeacherSprite(_teacher.Stats.Sprite);
            _hurtQueued    = false;
        }

        // Detach the HUD from the bus when the battle is popped, so its handler
        // (and this whole BattleState) can be garbage-collected instead of the
        // EventBus keeping a finished fight alive.
        public override void OnExit()
        {
            _hud.Unsubscribe();

            // attack might have been cut off mid loop (player died while
            // dodging), don't let it keep playing after the fight
            SoundManager.StopAllLoops();
        }

        public override void Update(GameTime gameTime)
        {
            // keeps drifting in every phase so the teacher never freezes
            _teacherSprite.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

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

                case BattlePhase.ShowingDamage:
                    UpdateDamageDisplay(gameTime);
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

            // the current attack can pan/shake the view. draw time only, the
            // hitboxes never move, so collision always matches what you see.
            // stays identity for attacks that don't use it
            bool dodging = _phase == BattlePhase.Dodging && _dodgePhase != null;
            Matrix view = Matrix.Identity;
            if (dodging)
            {
                Vector2 cam = _dodgePhase.CameraOffset;
                view = Matrix.CreateTranslation(-cam.X, -cam.Y, 0f);
            }

            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: view);

            // attacks can hide the HP bar completely (napoleon does)
            if (!dodging || !_dodgePhase.HudHidden)
                _hud.Draw(spriteBatch, Game.DialogueFont, Game.PixelTexture);

            if (_phase == BattlePhase.Dodging)
            {
                _dodgePhase.Draw(spriteBatch, Game.PixelTexture);
            }
            else
            {
                // not drawn while dodging, attacks like napoleon take over the
                // whole screen and it would just clash
                _teacherSprite.Draw(spriteBatch, TeacherAnchor);

                // slash/number/HP bar, only alive right after a hit
                _damageDisplay.Draw(spriteBatch, Game.PixelTexture);

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
            // an attack holds the turn while the slash/number/HP bar play out,
            // everything else goes straight to the enemy's turn
            if (_playerChoice == PlayerMoveList.Attack)
            {
                _teacher.TakeDamage(_pendingAttackDamage);
                _damageDisplay.Show(_pendingAttackDamage, _teacher.CurrentHp, _teacher.MaxHp,
                    TeacherBounds, WideBoxRect);

                // the slash sound is the same length as the animation so they
                // run together
                SoundManager.Play("slash");

                // he reacts once the slash lands, not while it's still swinging
                _hurtQueued = true;

                _playerChoice = null;
                _phase = BattlePhase.ShowingDamage;
                return;
            }
            // act/spare/item already did their thing in their Activate callbacks

            _playerChoice = null;
            BeginEnemyTurn();
        }

        // ── Phase: ShowingDamage ─────────────────────────────────────────────

        // holds while the slash, number and HP bar play, then carries on
        private void UpdateDamageDisplay(GameTime gameTime)
        {
            _damageDisplay.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            // hurt face + shake + the hit sound all fire the moment the slash ends
            if (_hurtQueued && _damageDisplay.SlashDone)
            {
                _teacherSprite.Hurt();
                SoundManager.Play("damage");
                _hurtQueued = false;
            }

            if (!_damageDisplay.IsFinished) return;

            _damageDisplay.Hide();
            BeginEnemyTurn();
        }

        // picks the teacher's attack and shrinks the box down for it
        private void BeginEnemyTurn()
        {
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
    }

    public enum PlayerMoveList { Attack, Act, Item, Spare }

    public enum BattlePhase { SelectingMove, ActionMenu, AttackMinigame, ExecutingTurn, ShowingDamage, BoxTransition, Dodging, BattleOver }
}
