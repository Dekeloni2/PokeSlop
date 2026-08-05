using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FinalProject.Battle;
using FinalProject.Battle.Patterns;
using FinalProject.Core;
using FinalProject.Core.Audio;
using FinalProject.Core.Graphics;
using FinalProject.Core.StateMachine;
using FinalProject.Core.Text;
using FinalProject.Data;
using FinalProject.Events;
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

        // where he stands during an attack that shows him, above whatever the
        // arena has resized itself to rather than the menu box
        private Vector2 DodgeTeacherAnchor => new Vector2(
            _dodgePhase.CurrentBox.Center.X - _teacherSprite.Size.X / 2f,
            _dodgePhase.CurrentBox.Top - _teacherSprite.Size.Y - 8f);

        // his rect on screen, the damage display hangs the slash/number/bar off it
        private Rectangle TeacherBounds => new Rectangle(
            (int)TeacherAnchor.X, (int)TeacherAnchor.Y,
            (int)_teacherSprite.Size.X, (int)_teacherSprite.Size.Y);

        // slash + damage number + the teacher's HP bar, shown right after a hit
        private readonly DamageDisplay _damageDisplay = new();

        // set when an attack lands, spent once the slash finishes
        private bool _hurtQueued;

        // what the teacher says. each action keeps its own place in its list, so
        // attacking twice steps the attack lines regardless of ACTs in between
        private readonly SpeechBubble _bubble = new();
        private readonly Dictionary<PlayerMoveList, int> _dialogueIndex = new();

        // which way the fight ended. a defeat is the sprite coming apart (see
        // TeacherSprite.Dust), a spare is a burst of smoke over a frozen pose
        private const float SpareSmokeSeconds = 1.714f; // matches snd_vaporized
        private const float SpareSmokeMinSpeed = 60f;   // px/sec outward
        private const float SpareSmokeSpeedRange = 120f;
        private const float SpareSmokeSpread   = 0.3f;  // of his size, where puffs start
        private const float SpareSmokeLife     = 1.4f;
        private const float SpareSmokeFadeIn   = 0.15f;
        private const int   SpareSmokeMinSize  = 14;
        private const int   SpareSmokeSizeRange = 20;
        private readonly ParticleSystem _spareSmoke = new();
        private float _spareLeft;

        private bool _endingSpared;
        private bool _endingStarted;

        // the soul-shatter death effect, created when the player's HP hits 0
        private SoulShatter _soulShatter;

        // his ultimate is a one-off, see PickEnemyMove
        private bool _ultimateUsed;

        // where a sequential teacher is up to in his move list
        private int _moveIndex;

        // a non-sequential teacher (David, Dorbendor) picks at random, so
        // this is what keeps that random pick from landing on the same move
        // twice in a row — see PickEnemyMove
        private MoveData _lastNormalMove;

        // the turn being dodged came off the lesson plan rather than an ACT, so
        // getting through it clean is allowed to advance a gated teacher
        private bool _dodgingLesson;

        // the turn currently being dodged is the ultimate. once he's fired his
        // best shot he's out of material, so surviving it opens up mercy
        private bool _ultimateDodging;

        // he's said his piece and stopped defending himself. any hit that lands
        // after this finishes him, however badly it was timed
        private bool _yielded;
        private bool _yieldStarted; // the speech has been queued into the bubble

        // a sequential teacher with no Yield block (Yakir) doesn't get a last
        // stand — he's just out of lesson plan. Rather than fall through to
        // his hardest normal move and frustrate a player who's already
        // survived the ultimate, any attack that lands from here finishes
        // him too, same as a true yield. Unlike a true yield he isn't done
        // fighting: ACT/ITEM (or a miss) just gets the ultimate thrown at
        // him again next turn instead of moving on. See UpdateDodging.
        private bool _finishingBlowReady;

        // an attack waiting behind its intro cutscene, started once he's done
        private IBulletPattern _pendingPattern;
        private MoveData       _pendingMove;

        // the move currently being dodged, kept so its Outro can be found once
        // the attack finishes. null on the provoked path, which has no move
        private MoveData    _dodgingMove;
        private string      _pendingOutro;    // queued line, nulled once the bubble has it
        private BattlePhase _phaseAfterOutro; // where the turn was headed before the outro
        private const int YieldedHitDamage = 9999;

        // whether anything has got through all fight. drives the extra line in
        // his yield speech, so it has to survive across every dodge phase
        private bool _tookDamage;

        // an ACT got under his skin, so this pattern jumps the queue next turn.
        // the lesson he skipped isn't lost, the sequence picks up where it was
        private string _provokedPattern;

        // an ACT with its own comeback, used instead of his usual onAct line
        // for one turn. cleared by PrepareDialogue once it's been said
        private string _actSpeech;

        // last line shown in his bubble mid attack, so it only rebuilds on change
        private string _lastDodgeSpeech;

        // the win text in the box, then a fade to black on the way out
        private const float FadeSeconds = 0.3f;
        private readonly Typewriter _victoryTyper = new();
        private bool  _victoryShown;
        private bool  _fading;
        private float _fadeT;

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
            _endingStarted = false;
            _ultimateUsed  = false;
            _ultimateDodging = false;
            _dodgingLesson   = false;
            _moveIndex       = 0;
            _lastNormalMove  = null;
            _yielded         = false;
            _yieldStarted    = false;
            _finishingBlowReady = false;
            _tookDamage      = false;
            _pendingPattern  = null;
            _pendingMove     = null;
            _dodgingMove     = null;
            _pendingOutro    = null;
            _provokedPattern = null;
            _actSpeech       = null;
            // patterns that escalate the more they're thrown count their own
            // uses in a static, so a new fight has to start them from zero
            CloverbytePattern.ResetUseCount();
            ChessPattern.ResetBoard();
            _moveIndex     = 0;
            _lastDodgeSpeech = null;
            _victoryShown  = false;
            _fading        = false;
            _fadeT         = 0f;
            _spareLeft     = 0f;
            _dialogueIndex.Clear();
            _spareSmoke.Clear();

            _bubble.BeepSound = _teacher.Stats.SpeakingVoice;

            // his theme runs for the whole fight, named in his JSON
            if (!string.IsNullOrEmpty(_teacher.Stats.Theme))
                SoundManager.PlayMusic(_teacher.Stats.Theme, true, _teacher.Stats.ThemeVolume);
        }

        // Detach the HUD from the bus when the battle is popped, so its handler
        // (and this whole BattleState) can be garbage-collected instead of the
        // EventBus keeping a finished fight alive.
        public override void OnExit()
        {
            _hud.Unsubscribe();

            // attack might have been cut off mid loop (player died while
            // dodging)
            SoundManager.StopAllLoops();

            // safety net, the fight can also end by the player dying which
            // never reaches BeginEnding
            SoundManager.StopMusic();
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

                case BattlePhase.TurnFeedback:
                    UpdateDamageDisplay(gameTime);
                    break;

                case BattlePhase.BoxTransition:
                    UpdateBoxTransition(gameTime);
                    break;

                case BattlePhase.Dodging:
                    UpdateDodging(gameTime);
                    break;

                case BattlePhase.MoveIntro:
                    UpdateMoveIntro(gameTime);
                    break;

                case BattlePhase.MoveOutro:
                    UpdateMoveOutro(gameTime);
                    break;

                case BattlePhase.Yielding:
                    UpdateYielding(gameTime);
                    break;

                case BattlePhase.Ending:
                    UpdateEnding(gameTime);
                    break;

                case BattlePhase.PlayerDying:
                    UpdatePlayerDying(gameTime);
                    break;

                case BattlePhase.BattleOver:
                    FinishBattle(gameTime);
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(Color.Black);

            // player death takes over the whole screen — just the shatter on black
            if (_phase == BattlePhase.PlayerDying)
            {
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _soulShatter.Draw(spriteBatch, SpriteManager.GetSprite("soul_lost"));
                spriteBatch.End();
                return;
            }

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

            // outside a dodge nothing else draws over this spot, so the usual
            // draw-early order is fine
            if (!dodging)
                _hud.Draw(spriteBatch, Game.DialogueFont, Game.PixelTexture);

            if (_phase == BattlePhase.Dodging)
            {
                // some attacks put him on screen above the arena and talk through
                // the fight, rather than the arena being the whole show
                if (_dodgePhase.TeacherVisible)
                {
                    Vector2 anchor = DodgeTeacherAnchor;
                    _teacherSprite.Draw(spriteBatch, anchor);
                    _bubble.Draw(spriteBatch, Game.DialogueFont,
                        new Vector2(anchor.X + _teacherSprite.Size.X + 4, anchor.Y + 24));
                }

                _dodgePhase.Draw(spriteBatch, Game.PixelTexture, Game.DialogueFont);

                // drawn last during a dodge, after the box and everything in
                // it — some attacks (Yakir's lessons) size their box low
                // enough to reach the bar's row, and this way the box goes
                // under the bar instead of its border/bullets/buttons
                // clipping through it. attacks can still hide it completely
                // outright (napoleon does)
                if (!_dodgePhase.HudHidden)
                    _hud.Draw(spriteBatch, Game.DialogueFont, Game.PixelTexture);
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

                // the win text takes the box over once the fight is decided
                if (_phase == BattlePhase.Ending && _victoryShown)
                {
                    Rectangle box = _box.Current;
                    spriteBatch.DrawString(Game.DialogueFont, _victoryTyper.VisibleText,
                        new Vector2(box.X + 16, box.Y + 16), Color.White,
                        0f, Vector2.Zero, NarrationTextScale, SpriteEffects.None, 0f);
                }


                // over him, he stays frozen underneath while it clears
                _spareSmoke.Draw(spriteBatch, Game.PixelTexture);

                // only up once he's actually been hit, it manages its own state
                _bubble.Draw(spriteBatch, Game.DialogueFont,
                    new Vector2(TeacherBounds.Right + 4, TeacherBounds.Top + 24));

                // buttons stay on screen the whole time (they only disappear
                // while dodging), the soul only sits on them while choosing.
                // his last stand takes them away too — nothing to press until
                // he's finished talking
                if (_phase != BattlePhase.Yielding && _phase != BattlePhase.MoveIntro
                                                   && _phase != BattlePhase.MoveOutro)
                    _menu.Draw(spriteBatch, showSoul: _phase == BattlePhase.SelectingMove);
            }

            // over the top of everything on the way out to the overworld
            if (_fading)
                spriteBatch.Draw(Game.PixelTexture,
                    new Rectangle(0, 0, GameSettings.WindowWidth, GameSettings.WindowHeight),
                    Color.Black * MathHelper.Clamp(_fadeT, 0f, 1f));

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
            // two routes out of a fight, whichever the player is further along
            // is the one he reacts to. mercy progress is the spare%, the other
            // route's progress is damage dealt, so compare against that and not
            // against the HP he has left
            float hp       = CurrentHpPercent();
            float damage   = 100f - hp;
            float spare    = _teacher.SparePercent;
            var   bySpare  = _teacher.Stats.TurnNarrationBySpare;

            string narrationText = (bySpare != null && bySpare.Count > 0 && spare > damage)
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

        // once he's yielded there's nothing left to talk him round with, so his
        // usual list gives way to whatever short one his JSON leaves behind
        private IReadOnlyList<ActOption> CurrentActOptions
        {
            get
            {
                IReadOnlyList<ActOption> yieldOptions = _teacher.Stats.Yield?.ActOptions;

                return _yielded && yieldOptions != null && yieldOptions.Count > 0
                    ? yieldOptions
                    : _teacher.Stats.ActOptions;
            }
        }

        private List<MenuOption> BuildActOptionsPage()
        {
            var page = new List<MenuOption>();
            foreach (ActOption opt in CurrentActOptions)
                page.Add(new MenuOption(opt.Name, () =>
                {
                    _teacher.IncreaseSparePercent(opt.SpareGain);
                    if (opt.ForcesPattern != null) _provokedPattern = opt.ForcesPattern;
                    if (opt.Speech != null)        _actSpeech       = opt.Speech;
                    PushMessageSequence(opt.GetMessages(CurrentHpPercent()), PlayerMoveList.Act);
                }));
            return page;
        }

        // opening ITEM with no items doesn't waste the turn, it just backs
        // out (same as pressing X). Only actually using an item costs a turn.
        // Armor doesn't belong on this list at all — it's equipped from the
        // overworld menu (see OverworldMenu.EquipArmorFromInventory), not
        // eaten mid fight for 0 HP
        private List<MenuOption> BuildItemRootPage()
        {
            List<ItemData> consumables = Game.PlayerData.Inventory.FindAll(item => !item.IsEquipment);

            if (consumables.Count == 0)
            {
                return new List<MenuOption>
                {
                    new MenuOption("You have no items.", () => _phase = BattlePhase.SelectingMove)
                };
            }

            var page = new List<MenuOption>();
            foreach (ItemData item in consumables)
                page.Add(new MenuOption(item.Name, () => UseItem(item)));
            return page;
        }

        private void UseItem(ItemData item)
        {
            Game.PlayerData.Heal(item.HealAmount);
            SoundManager.Play(SoundManager.HealSound);
            Game.PlayerData.Inventory.Remove(item);
            PushMessageSequence(
                new List<string> { $"You ate the {item.Name}. Restored {item.HealAmount} HP." },
                PlayerMoveList.Item);
        }

        // his name goes yellow once sparing him would actually work, so the
        // player can tell without having to try it
        private List<MenuOption> BuildSpareRootPage()
        {
            bool sparable = _teacher.SparePercent >= _teacher.Stats.SpareSuccessAt;

            return new List<MenuOption>
            {
                new MenuOption(_teacher.Name,
                    () => _actionMenu.Push(BuildSpareConfirmPage()),
                    sparable ? Color.Yellow : Color.White)
            };
        }

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
            // a '|' gives the rest of the line its own box, same as the speech
            // bubble and the overworld. done here so every caller gets it
            if (index == 0) messages = SplitPages(messages);

            bool isLast = index >= messages.Count - 1;
            _actionMenu.Push(new List<MenuOption>
            {
                new MenuOption(messages[index], isLast
                    ? () => FinalizeChoice(finalChoice)
                    : () => PushMessageSequence(messages, finalChoice, index + 1))
            }, allowCancel: false, typewriter: true);
        }

        // one entry per box. stray breaks and blank entries are dropped, and an
        // empty result still yields one page so the turn can always be advanced
        private static List<string> SplitPages(List<string> messages)
        {
            var pages = new List<string>();

            foreach (string message in messages)
            {
                if (string.IsNullOrEmpty(message)) continue;

                foreach (string part in message.Split('|'))
                {
                    string trimmed = part.Trim();
                    if (trimmed.Length > 0) pages.Add(trimmed);
                }
            }

            if (pages.Count == 0) pages.Add(string.Empty);
            return pages;
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
            // once he's yielded he isn't defending himself — any strike that
            // connects finishes it, however badly it was timed. a clean miss
            // still misses
            _pendingAttackDamage = _attackMinigame.Missed ? 0
                : _yielded || _finishingBlowReady ? YieldedHitDamage
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
            // his line depends on what the player just did, so it's picked here
            // rather than before the choice was made
            PrepareDialogue(_playerChoice ?? PlayerMoveList.Act);

            // an attack holds the turn while the slash/number/HP bar play out,
            // everything else goes straight to the enemy's turn
            if (_playerChoice == PlayerMoveList.Attack)
            {
                _teacher.TakeDamage(_pendingAttackDamage);

                // that hit killed him, so the music cuts the instant his HP
                // updates, before the number and the bar even come up
                if (!_teacher.IsAlive)
                {
                    SoundManager.StopMusic();

                    // his defeat line takes over, so drop the attack banter
                    // instead of saying both
                    _bubble.Clear();
                }

                _damageDisplay.Show(_pendingAttackDamage, _teacher.CurrentHp, _teacher.MaxHp,
                    TeacherBounds, WideBoxRect);

                // the slash sound is the same length as the animation so they
                // run together
                SoundManager.Play("slash");

                // he reacts once the slash lands, not while it's still swinging
                _hurtQueued = true;

                _playerChoice = null;
                _phase = BattlePhase.TurnFeedback;
                return;
            }
            // act/spare/item already did their thing in their Activate callbacks

            _playerChoice = null;

            // same on a spare that actually worked, the farewell wins
            if (IsBattleOver()) _bubble.Clear();

            // he still gets his line on a turn where he wasn't hit. no damage
            // display is running so TurnFeedback just waits on the bubble
            if (_bubble.HasText)
            {
                _bubble.Begin();
                _phase = BattlePhase.TurnFeedback;
                return;
            }

            BeginEnemyTurn();
        }

        // ── Phase: TurnFeedback ─────────────────────────────────────────────

        // holds while the slash, number and HP bar play, then carries on
        private void UpdateDamageDisplay(GameTime gameTime)
        {
            _damageDisplay.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            // hurt face + shake + the hit sound all fire the moment the slash ends,
            // and that's when he starts talking, not before he's been hit
            if (_hurtQueued && _damageDisplay.SlashDone)
            {
                // the killing blow rocks him harder and for longer
                _teacherSprite.Hurt(fatal: !_teacher.IsAlive);
                SoundManager.Play("damage");
                _bubble.Begin();
                _hurtQueued = false;
            }

            _bubble.Update(gameTime, Game.Input);

            // waits for the numbers, for the player to close the bubble, and for
            // the wobble to settle. that last one keeps the death shake from
            // being cut short by his final words starting
            if (!_damageDisplay.IsFinished || _bubble.IsActive || _teacherSprite.IsShaking) return;

            _damageDisplay.Hide();
            BeginEnemyTurn();
        }

        // picks the teacher's attack and shrinks the box down for it
        private void BeginEnemyTurn()
        {
            // the fight being over has to win over everything below, including
            // the yield check — that's the whole point of yielding, the player
            // gets to end it
            if (IsBattleOver())
            {
                // player death plays the soul shatter, the teacher going down gets a scene
                if (Game.PlayerData.IsAlive) BeginEnding();
                else                         BeginPlayerDeath();
                return;
            }

            // he's yielded and the player didn't finish it. he doesn't raise a
            // hand again either way, so the turn comes straight back rather
            // than picking a move
            if (_yielded)
            {
                _lastNarrationText = null;
                RefreshNarration();
                _phase = BattlePhase.SelectingMove;
                return;
            }

            IBulletPattern pattern;
            MoveData       move = null; // stays null on the provoked path below

            if (_provokedPattern != null)
            {
                // you poked the bear, the lesson plan can wait
                pattern = PatternRegistry.Resolve(_provokedPattern)();
                _provokedPattern = null;
                _ultimateDodging = false;
            }
            else
            {
                move = PickEnemyMove();
                _ultimateDodging = move.IsUltimate;
                pattern = move.CreatePattern();
            }

            // a move can announce itself first — buttons off, he talks, and the
            // attack only starts once the player has read it
            if (!string.IsNullOrWhiteSpace(move?.Intro))
            {
                _pendingPattern = pattern;
                _pendingMove    = move;

                _bubble.Prepare(move.Intro, Game.DialogueFont);
                _bubble.Begin();
                _phase = BattlePhase.MoveIntro;
                return;
            }

            StartDodge(pattern, move);
        }

        // the move comes along so patterns can read their authored lines out
        // of the teacher's JSON instead of holding them in code
        private void StartDodge(IBulletPattern pattern, MoveData move)
        {
            // a provoked pattern has no move behind it, so it isn't part of the
            // lesson plan and can't count as clearing one
            _dodgingLesson = move != null;
            _dodgingMove   = move;
            _dodgePhase = new DodgePhase(pattern, _teacher, Game.PlayerData, DodgeBoxRect, move, WideBoxRect);

            _box.ResizeTo(DodgeBoxRect, ShrinkSeconds);
            _phaseAfterTransition = BattlePhase.Dodging;
            _phase = BattlePhase.BoxTransition;
        }

        // ── Phase: MoveIntro ─────────────────────────────────────────────────

        // he announces the attack before throwing it. held here rather than
        // inside the pattern so the pattern doesn't have to know about the
        // bubble, and so the player reads it before the box shrinks
        private void UpdateMoveIntro(GameTime gameTime)
        {
            _bubble.Update(gameTime, Game.Input);
            if (_bubble.IsActive) return; // let him finish

            _bubble.Clear();
            StartDodge(_pendingPattern, _pendingMove);

            _pendingPattern = null;
            _pendingMove    = null;
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

            // one hit anywhere in the fight is enough to lose the flawless line
            if (_dodgePhase.PlayerHitCount > 0) _tookDamage = true;

            // refresh his bubble when the attack changes what he's saying, only
            // on a change so it isn't restarted every frame, then let it type
            if (_dodgePhase.TeacherVisible)
            {
                if (_dodgePhase.TeacherSpeech != _lastDodgeSpeech)
                {
                    _lastDodgeSpeech = _dodgePhase.TeacherSpeech;
                    _bubble.ShowTyping(_lastDodgeSpeech, Game.DialogueFont);
                }

                _bubble.UpdateText((float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            // stop early if the player died mid-dodge
            if (IsBattleOver())
            {
                // player death plays the soul shatter, the teacher going down gets a scene
                if (Game.PlayerData.IsAlive) BeginEnding();
                else                         BeginPlayerDeath();
                return;
            }

            if (_dodgePhase.IsFinished)
            {
                // read before the phase goes, the gate below needs it
                bool clearedCleanly = _dodgePhase.PlayerHitCount == 0;
                _dodgePhase = null;

                AdvanceGatedLesson(clearedCleanly);

                // he threw everything he had and you're still standing, so the
                // fight is winding down whether he likes it or not
                bool yielding = false;
                if (_ultimateDodging)
                {
                    _ultimateDodging = false;

                    // a teacher with a last stand hands mercy over at the end of
                    // it instead, so the buttons stay gone until he's finished
                    if (_teacher.Stats.Yield != null)
                    {
                        yielding = true;
                    }
                    else
                    {
                        _teacher.IncreaseSparePercent(100);

                        // no last stand to hand mercy over here, so he keeps
                        // fighting — but with nothing left to teach, it's the
                        // ultimate again (not whatever came next in the lesson
                        // plan) until the player finishes it one way or another
                        _finishingBlowReady = true;
                        _ultimateUsed       = false;
                    }
                }

                // a talking attack leaves its line up, drop it or it hangs
                // around into the menu phase
                _bubble.Clear();
                _lastDodgeSpeech = null;
                _box.ResizeTo(WideBoxRect, GrowSeconds);
                // a finished enemy turn is a fresh turn — force the narration to
                // re-type. Prime it to zero now (during the box transition, before
                // the first SelectingMove draw) so there's no one-frame flash of
                // the fully-typed text from last turn. Unlike backing out of a
                // submenu, which keeps the guard and shows the text instantly.
                _lastNarrationText = null;
                RefreshNarration();

                BattlePhase next = yielding ? BattlePhase.Yielding : BattlePhase.SelectingMove;

                // a move can sign off the way it announced itself. the line lands
                // between the attack ending and whatever comes next — on the
                // ultimate that puts it just before his last stand
                if (!string.IsNullOrWhiteSpace(_dodgingMove?.Outro))
                {
                    _pendingOutro    = _dodgingMove.Outro;
                    _phaseAfterOutro = next;
                    next             = BattlePhase.MoveOutro;
                }

                _dodgingMove = null;
                _phaseAfterTransition = next;
                _phase = BattlePhase.BoxTransition;
            }
        }

        // ── Phase: MoveOutro ─────────────────────────────────────────────────

        // the mirror of MoveIntro — he gets the last word once the attack is
        // over. buttons stay off screen while he talks, and the turn only hands
        // back (or his last stand only begins) once the player has read it
        private void UpdateMoveOutro(GameTime gameTime)
        {
            // queued here rather than at the hand-off so the line starts typing
            // with the box already back at full width
            if (_pendingOutro != null)
            {
                _bubble.Prepare(_pendingOutro, Game.DialogueFont);
                _bubble.Begin();
                _pendingOutro = null;
            }

            _bubble.Update(gameTime, Game.Input);
            if (_bubble.IsActive) return; // let him finish

            _bubble.Clear();
            _phase = _phaseAfterOutro;
        }

        // ── Phase: Yielding ──────────────────────────────────────────────────

        // he's out of attacks and out of argument. the buttons come off screen
        // while he talks, then mercy is his to give and the player picks how
        // this ends. he doesn't defend himself afterwards either way
        private void UpdateYielding(GameTime gameTime)
        {
            if (!_yieldStarted)
            {
                _yieldStarted = true;

                // his theme has carried the whole fight, and it stops with him.
                // what he says next plays out in silence
                SoundManager.StopMusic();

                // the extra line is only earned by getting through untouched
                _bubble.Prepare(_teacher.Stats.Yield.BuildSpeech(!_tookDamage), Game.DialogueFont);
                _bubble.Begin();
            }

            _bubble.Update(gameTime, Game.Input);
            if (_bubble.IsActive) return; // let him finish

            _yielded = true;
            _teacher.IncreaseSparePercent(100);

            // his mercy state just changed, so the narration under the buttons
            // has to catch up before it's drawn again
            _lastNarrationText = null;
            RefreshNarration();
            _phase = BattlePhase.SelectingMove;
        }

        // ── Phase: PlayerDying ───────────────────────────────────────────────

        // the soul shattering on black. plays out, then the fight ends. the
        // GAME OVER screen will hook in where this currently pops the battle
        private void BeginPlayerDeath()
        {
            // everything, not just loops and music — a one-shot from whatever
            // was mid-swing would otherwise ring out over the shatter.
            // has to happen before SoulShatter below, that plays its own cue
            SoundManager.StopAll();

            Vector2 soulPos = _dodgePhase != null
                ? _dodgePhase.HitboxPosition - _dodgePhase.CameraOffset
                : new Vector2(GameSettings.WindowWidth / 2f, GameSettings.WindowHeight / 2f);

            _soulShatter = new SoulShatter(soulPos);
            _phase = BattlePhase.PlayerDying;
        }

        private void UpdatePlayerDying(GameTime gameTime)
        {
            _soulShatter.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            if (_soulShatter.IsFinished)
                StateManager.Replace(new GameOverState(Game, StateManager));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        // A beat of held black between the battle's fade-out and the ending, so
        // the phone doesn't start ringing on the same frame the fade landed.
        private const float EndingHoldSeconds = 1.2f;
        private float _endingHold;

        // Every fight but the last one just drops back to the overworld. The
        // final teacher (endsGame in his JSON) rolls straight into the ending —
        // the screen is already fully black here, so the hold reads as a pause
        // rather than a freeze. Losing never reaches this phase; a death goes to
        // GameOverState from PlayerDying instead.
        private void FinishBattle(GameTime gameTime)
        {
            if (!_teacher.Stats.EndsGame)
            {
                StateManager.Pop();
                return;
            }

            _endingHold += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_endingHold < EndingHoldSeconds) return;

            StateManager.Replace(new EndingState(Game, StateManager));
        }

        private bool IsBattleOver()
            => !Game.PlayerData.IsAlive || !_teacher.IsAlive || _teacher.IsSpared;

        // ── Phase: Ending ────────────────────────────────────────────────────

        // his last words, then he either crumbles or freezes. the fight doesn't
        // pop until that has played out
        private void BeginEnding()
        {
            TeacherDialogue d = _teacher.Stats.Dialogue;
            bool spared = _teacher.IsSpared || _teacher.IsAlive; // alive = mercy, not a kill

            string line = spared ? d?.OnSpared : d?.OnDefeat;

            // the fight is decided, so the music cuts here. his last words and
            // whatever happens to him after play out in silence
            SoundManager.StopMusic();

            _bubble.Prepare(line, Game.DialogueFont);
            _bubble.Begin();

            // he stops moving the moment the fight is decided, so he isn't
            // bobbing away while delivering his last words
            _teacherSprite.Freeze();

            _endingSpared = spared;

            // the fight is settled here, whichever way it went. announced rather
            // than written down directly so the run's bookkeeping isn't this
            // state's problem — see RouteTracker
            EventBus.Instance.Publish(new TeacherResolvedEvent(
                _teacher.Stats.Id, spared ? BattleOutcome.Spared : BattleOutcome.Killed));

            _phase = BattlePhase.Ending;
        }

        private void UpdateEnding(GameTime gameTime)
        {
            _bubble.Update(gameTime, Game.Input);
            if (_bubble.IsActive) return; // let him finish talking first

            // kick the visual off once, then wait for it
            if (!_endingStarted)
            {
                _endingStarted = true;

                // both endings vanish him, so both play it. the animations are
                // timed to this sound's length either way
                SoundManager.Play("vaporized");

                if (_endingSpared)
                {
                    _teacherSprite.Spare();
                    SpawnSpareSmoke();
                    _spareLeft = SpareSmokeSeconds;
                }
                else
                {
                    _teacherSprite.Dust();
                }
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _spareSmoke.Update(dt);

            // wait for the smoke to clear, or for him to finish crumbling
            if (_endingSpared)
            {
                if (_spareLeft > 0f) { _spareLeft -= dt; return; }
            }
            else if (!_teacherSprite.IsDustFinished)
            {
                return;
            }

            // pay out once, then the win text
            if (!_victoryShown)
            {
                _victoryShown = true;
                Game.PlayerData.Money += _teacher.Stats.GoldReward;
                _victoryTyper.SetText($"* YOU WON!\n* You earned {_teacher.Stats.GoldReward} gold.");
            }

            if (_fading)
            {
                _fadeT += dt / FadeSeconds;
                if (_fadeT >= 1f) _phase = BattlePhase.BattleOver;
                return;
            }

            _victoryTyper.Update(dt);

            if (!Game.Input.IsKeyPressed(Keys.Z)) return;

            // first Z finishes the typing, second starts the fade out
            if (!_victoryTyper.IsFullyShown) { _victoryTyper.SkipToEnd(); return; }

            _fading = true;
        }

        // a puff of smoke bursting outward from him when he's spared. spreads in
        // every direction, drifts apart and fades while he stays frozen underneath
        private void SpawnSpareSmoke()
        {
            Rectangle b = TeacherBounds;
            Spritesheet smoke = SpriteManager.GetSprite("smoke");
            Texture2D tex = smoke?.Texture;
            int smokeAmount = 30;

            for (int i = 0; i < smokeAmount; i++)
            {
                float angle = (float)(_rng.NextDouble() * MathHelper.TwoPi);
                float speed = SpareSmokeMinSpeed + (float)_rng.NextDouble() * SpareSmokeSpeedRange;

                var pos = new Vector2(
                    b.Center.X + ((float)_rng.NextDouble() * 2f - 1f) * b.Width  * SpareSmokeSpread,
                    b.Center.Y + ((float)_rng.NextDouble() * 2f - 1f) * b.Height * SpareSmokeSpread);

                var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

                _spareSmoke.Spawn(pos, vel, SpareSmokeLife,
                    SpareSmokeMinSize + _rng.Next(SpareSmokeSizeRange),
                    Color.White, SpareSmokeFadeIn, tex);
            }
        }

        // The ultimate is saved for the turn the fight is about to end: either the
        // player just ACTed him into being sparable, or one more clean hit would
        // finish him. A teacher working through a lesson plan goes by the plan
        // instead, see IsUltimateTime. It only ever fires once, so a missed swing
        // afterwards just gets a normal attack instead of his big finish again.
        private MoveData PickEnemyMove()
        {
            var normal = new List<MoveData>();
            MoveData ultimate = null;

            foreach (MoveData m in _teacher.Moves)
            {
                if (m.IsUltimate) ultimate = m;
                else              normal.Add(m);
            }

            if (ultimate != null && !_ultimateUsed && IsUltimateTime(normal.Count))
            {
                _ultimateUsed = true;
                return ultimate;
            }

            // no normal moves defined, fall back to whatever he has
            if (normal.Count == 0) return _teacher.Moves[_rng.Next(_teacher.Moves.Count)];

            // lessons play in JSON order and then hold on the last one. wrapping
            // would restart the course after his big finish, which reads as him
            // forgetting what he just taught
            if (_teacher.Stats.SequentialMoves)
            {
                int i = Math.Min(_moveIndex, normal.Count - 1);

                // a gated teacher doesn't step forward here — UpdateDodging
                // does it, and only if the player got through untouched, so a
                // lesson repeats until it actually lands
                if (!_teacher.Stats.GatedMoves) _moveIndex++;
                return normal[i];
            }

            MoveData picked = PickRandomExcluding(normal, _lastNormalMove);
            _lastNormalMove = picked;
            return picked;
        }

        // Picks at random, but not the same move that just went last turn (so
        // David and Dorbendor don't throw the same attack twice back to
        // back) — unless there's only the one move to pick from at all, in
        // which case there's nothing else it could be.
        private MoveData PickRandomExcluding(List<MoveData> options, MoveData exclude)
        {
            if (options.Count <= 1 || exclude == null)
                return options[_rng.Next(options.Count)];

            var candidates = new List<MoveData>(options);
            candidates.Remove(exclude);

            return candidates[_rng.Next(candidates.Count)];
        }

        // a gated teacher only moves on when the player got through the last
        // attack without being hit, so each lesson repeats until it's understood
        // rather than scrolling past on a timer. clearing the last one is what
        // opens up mercy — the same handover surviving an ultimate gets
        private void AdvanceGatedLesson(bool clearedCleanly)
        {
            if (!_teacher.Stats.SequentialMoves || !_teacher.Stats.GatedMoves) return;
            if (!_dodgingLesson || !clearedCleanly) return;

            _moveIndex++;

            if (_moveIndex >= NormalMoveCount)
                _teacher.IncreaseSparePercent(100);
        }

        // his attacks minus the ultimate, which isn't part of a lesson plan
        private int NormalMoveCount
        {
            get
            {
                int n = 0;
                foreach (MoveData m in _teacher.Moves) if (!m.IsUltimate) n++;
                return n;
            }
        }

        // a teacher with a lesson plan holds his ultimate until he's actually
        // taught everything, so how fast the player hits stops deciding which
        // material gets seen. everyone else goes by how close the fight is
        private bool IsUltimateTime(int normalCount)
            => _teacher.Stats.SequentialMoves
                ? _moveIndex >= normalCount
                : IsFightAboutToEnd();

        private bool IsFightAboutToEnd()
        {
            bool sparableNow = _teacher.SparePercent >= _teacher.Stats.SpareSuccessAt;

            // a teacher can name the HP he panics at. without one it's "one more
            // clean hit would finish him", which drifts with the player's attack
            int threshold   = _teacher.Stats.UltimateAtHp ?? Game.PlayerData.Attack;
            bool nearlyDown = _teacher.CurrentHp <= threshold;

            return sparableNow || nearlyDown;
        }

        // picks his line for whatever the player just did. an HP line overrides
        // everything, otherwise it takes the next entry from that action's own
        // list and advances only that one
        private void PrepareDialogue(PlayerMoveList choice)
        {
            TeacherDialogue d = _teacher.Stats.Dialogue;
            if (d == null) { _bubble.Clear(); return; }

            // using an item counts as acting, same lines and same place in them,
            // unless the teacher bothers to define its own item lines
            if (choice == PlayerMoveList.Item && (d.OnItem == null || d.OnItem.Count == 0))
                choice = PlayerMoveList.Act;

            string line = null;

            // an ACT that has its own comeback wins over everything else, it's
            // a direct answer to what the player just did
            if (_actSpeech != null)
            {
                line       = _actSpeech;
                _actSpeech = null;
            }

            if (string.IsNullOrEmpty(line) && d.ByHp != null && d.ByHp.Count > 0)
                line = PercentThresholdText.Resolve(d.ByHp, CurrentHpPercent());

            if (string.IsNullOrEmpty(line))
            {
                IReadOnlyList<string> list = choice switch
                {
                    PlayerMoveList.Attack => d.OnAttack,
                    PlayerMoveList.Act    => d.OnAct,
                    PlayerMoveList.Item   => d.OnItem,
                    PlayerMoveList.Spare  => d.OnSpare,
                    _                     => null
                };

                if (list != null && list.Count > 0)
                {
                    _dialogueIndex.TryGetValue(choice, out int i);
                    line = list[i % list.Count];
                    _dialogueIndex[choice] = i + 1;
                }
            }

            if (string.IsNullOrEmpty(line)) _bubble.Clear();
            else                            _bubble.Prepare(line, Game.DialogueFont);
        }

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

    public enum BattlePhase { SelectingMove, ActionMenu, AttackMinigame, ExecutingTurn, TurnFeedback, BoxTransition, MoveIntro, Dodging, MoveOutro, Yielding, Ending, PlayerDying, BattleOver }
}
