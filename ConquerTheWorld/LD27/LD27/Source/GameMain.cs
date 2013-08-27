using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ConquerTheWorld {
  public class GameMain : Game {
    readonly GraphicsDeviceManager Graphics;
    const int NUM_REGIONS = 10;
    public GameMain() {
      Graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
      Graphics.PreferredBackBufferWidth = 1024;
      Graphics.PreferredBackBufferHeight = 768;
      Graphics.ApplyChanges();
      IsMouseVisible = true;
    }

    Map Map;

    protected override void Initialize() {
      var inputManager = new InputManager(this);
      Components.Add(inputManager);
      Services.AddService(typeof(InputManager), inputManager);
      Services.AddService(typeof(Random), new Random());

      base.Initialize();
    }

    const string EASY_TEXT = "EASY";
    const string NORMAL_TEXT = "NORMAL";
    const string HARD_TEXT = "HARD";

    SpriteFont SmallFont;
    SpriteFont LargeFont;
    Texture2D AttackTexture;
    Texture2D DefendTexture;
    SoundEffect GongSound;

    protected override void LoadContent() {
      Services.AddService(typeof(SpriteBatch), new SpriteBatch(GraphicsDevice));
      SmallFont = Content.Load<SpriteFont>("BitstreamVeraSmall");
      LargeFont = Content.Load<SpriteFont>("BitstreamVeraLarge");
      AttackTexture = Content.Load<Texture2D>("Attack");
      DefendTexture = Content.Load<Texture2D>("Defend");
      GongSound = Content.Load<SoundEffect>("Gong");

      Vector2 easyTextSize = SmallFont.MeasureString(EASY_TEXT);
      EasyButton = new Rectangle(150, 700, (int)easyTextSize.X, (int)easyTextSize.Y);
      Vector2 normalTextSize = SmallFont.MeasureString(NORMAL_TEXT);
      NormalButton = new Rectangle(EasyButton.Right + 100, 700, (int)normalTextSize.X, (int)normalTextSize.Y);
      Vector2 hardTextSize = SmallFont.MeasureString(HARD_TEXT);
      HardButton = new Rectangle(NormalButton.Right + 100, 700, (int)hardTextSize.X, (int)hardTextSize.Y);
    }

    protected override void UnloadContent() {}

    protected override void Update(GameTime gameTime) {
      base.Update(gameTime);

      var inputManager = (InputManager) Services.GetService(typeof(InputManager));

      if (inputManager.KeyPressed(Keys.Escape)) {
        Exit();
      }

      if (!GameStarted && inputManager.MouseIsOnClient() && inputManager.MouseReleased()) {
        if (EasyButton.Contains(new Point((int)inputManager.MousePosition().X, (int)inputManager.MousePosition().Y))) {
          StartGame(2500);
        } else if (NormalButton.Contains(new Point((int)inputManager.MousePosition().X, (int)inputManager.MousePosition().Y))) {
          StartGame(1500);
        } else if (HardButton.Contains(new Point((int)inputManager.MousePosition().X, (int)inputManager.MousePosition().Y))) {
          StartGame(1000);
        }
      } else if (GameEnded && inputManager.MouseIsOnClient() && inputManager.MouseReleased()
                 && GameEndedLockoutTimerMS < 0) {
        TitleScreen();
      }

      if (GameStarted) {
        TimeElapsedMS += gameTime.ElapsedGameTime.Milliseconds;
      }
      if (GameEnded) {
        GameEndedLockoutTimerMS -= gameTime.ElapsedGameTime.Milliseconds;
      }
    }

    Rectangle EasyButton;
    Rectangle NormalButton;
    Rectangle HardButton;

    void TitleScreen() {
      GameStarted = false;
      GameEnded = false;
      Components.Remove(Map);
    }

    void StartGame(int aiTickTime) {
      Map = new Map(this, NUM_REGIONS, new Viewport(0, 0, 1024 - 200, 768), aiTickTime);
      Components.Add(Map);

      GameStarted = true;
      GameEnded = false;
      TimeElapsedMS = 0;
      OldTimeString = "10";
    }

    void StopGame() {
      if (GameEnded) {
        return;
      }
      GameEnded = true;
      GameEndedLockoutTimerMS = 2000;
      Map.Enabled = false;
    }

    int GameEndedLockoutTimerMS;

    bool GameEnded;

    int TimeElapsedMS { get; set; }

    bool GameStarted;

    protected override void Draw(GameTime gameTime) {
      GraphicsDevice.Clear(Color.Black);
      base.Draw(gameTime);

      if (!GameStarted) {
        DrawTitle();
      } else {
        DrawScoreboard();
      }
    }

    string OldTimeString;
    void DrawScoreboard() {
      float timeLeft = 10 - TimeElapsedMS / 1000f;
      if (timeLeft <= 0) {
        StopGame();
        timeLeft = 0;
      }

      string timeLeftString = string.Format("{0:N0}", timeLeft);
      if (OldTimeString != timeLeftString) {
        OldTimeString = timeLeftString;
        GongSound.Play();
      }
      string scoreString = string.Format("{0}", Map.NumPlayerOwnedRegions);

      var spriteBatch = (SpriteBatch) Services.GetService(typeof(SpriteBatch));
      spriteBatch.Begin();

      if (GameEnded) {
        string winLoseText;
        Color winLoseColor;
        if (Map.NumPlayerOwnedRegions > 5) {
          winLoseText = "WIN";
          winLoseColor = Color.Blue;
        } else if (Map.NumPlayerOwnedRegions == 5) {
          winLoseText = "TIED";
          winLoseColor = Color.White;
        } else {
          winLoseText = "LOST";
          winLoseColor = Color.Red;
        }

        spriteBatch.DrawString(
            SmallFont, "YOU", new Vector2(1024 - 200, 0), winLoseColor, 0, Vector2.Zero, 1,
            SpriteEffects.None, 0);
        spriteBatch.DrawString(
            SmallFont, winLoseText, new Vector2(1024 - 200, 80), winLoseColor, 0, Vector2.Zero, 1,
            SpriteEffects.None, 0);

      } else {
        spriteBatch.DrawString(
            SmallFont, "TIME", new Vector2(1024 - 170, 0), Color.White, 0, Vector2.Zero, 1,
            SpriteEffects.None, 0);
        Vector2 timeLeftStringSize = LargeFont.MeasureString(timeLeftString);
        spriteBatch.DrawString(
            LargeFont, timeLeftString, new Vector2(1024 - 200 + (200 - timeLeftStringSize.X) / 2, 80), Color.White, 0, Vector2.Zero,
            1, SpriteEffects.None, 0);
      }

      spriteBatch.DrawString(
          SmallFont, "SCORE", new Vector2(1024 - 190, 768 - 380), Color.White, 0, Vector2.Zero, 1,
          SpriteEffects.None, 0);
      Vector2 scoreStringSize = LargeFont.MeasureString(scoreString);
      spriteBatch.DrawString(
          LargeFont, scoreString, new Vector2(1024 - 200 + (200 - scoreStringSize.X) / 2, 768 - 310), Color.Blue, 0, Vector2.Zero, 1,
          SpriteEffects.None, 0);

      spriteBatch.End();
    }

    void DrawTitle() {
      var spriteBatch = (SpriteBatch) Services.GetService(typeof(SpriteBatch));
      spriteBatch.Begin();
      Vector2 conquerSize = LargeFont.MeasureString("CONQUER");
      spriteBatch.DrawString(
          LargeFont, "CONQUER", new Vector2(1024 / 2 - conquerSize.X / 2, 20), Color.Blue, 0, Vector2.Zero, 1,
          SpriteEffects.None, 0);
      Vector2 theWorldSize = LargeFont.MeasureString("THE WORLD");
      spriteBatch.DrawString(
          LargeFont, "THE WORLD", new Vector2(1024 / 2 - theWorldSize.X / 2, 190), Color.Red, 0, Vector2.Zero, 1,
          SpriteEffects.None, 0);

      spriteBatch.DrawString(
          SmallFont, "Attack", new Vector2(150, 380), Color.Red, 0, Vector2.Zero, 1,
          SpriteEffects.None, 0);
      spriteBatch.Draw(AttackTexture, new Vector2(180, 460), Color.White);

      spriteBatch.DrawString(
          SmallFont, "Defend", new Vector2(590, 380), Color.Blue, 0, Vector2.Zero, 1,
          SpriteEffects.None, 0);
      spriteBatch.Draw(DefendTexture, new Vector2(620, 460), Color.White);

      const string INSTRUCTION_STRING = "Click on start region, then click on target region.";
      spriteBatch.DrawString(
          SmallFont, INSTRUCTION_STRING, new Vector2(40, 650), Color.Blue, 0, Vector2.Zero, .5f,
          SpriteEffects.None, 0);

      spriteBatch.DrawString(SmallFont, EASY_TEXT, new Vector2(EasyButton.Location.X, EasyButton.Location.Y), Color.Blue);
      spriteBatch.DrawString(SmallFont, NORMAL_TEXT, new Vector2(NormalButton.Location.X, NormalButton.Location.Y), Color.White);
      spriteBatch.DrawString(SmallFont, HARD_TEXT, new Vector2(HardButton.Location.X, HardButton.Location.Y), Color.Red);

      spriteBatch.End();
    }
  }
}
