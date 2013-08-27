using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ConquerTheWorld {
  public class InputManager : GameComponent {
    public InputManager(Game game) : base(game) {}

    public override void Update(GameTime gameTime) {
      base.Update(gameTime);
      OldKeyboardState = CurrentKeyboardState;
      CurrentKeyboardState = Keyboard.GetState();
      OldMouseState = CurrentMouseState;
      CurrentMouseState = Mouse.GetState();
    }

    public bool KeyDown(Keys key) {
      return CurrentKeyboardState.IsKeyDown(key);
    }

    public bool KeyUp(Keys key) {
      return CurrentKeyboardState.IsKeyUp(key);
    }

    public bool KeyPressed(Keys key) {
      return OldKeyboardState.IsKeyUp(key) && CurrentKeyboardState.IsKeyDown(key);
    }

    public bool KeyReleased(Keys key) {
      return OldKeyboardState.IsKeyDown(key) && CurrentKeyboardState.IsKeyUp(key);
    }

    public bool MouseDown() {
      return CurrentMouseState.LeftButton == ButtonState.Pressed;
    }

    public bool MouseUp() {
      return CurrentMouseState.LeftButton == ButtonState.Released;
    }

    public bool MousePressed() {
      return OldMouseState.LeftButton == ButtonState.Released
             && CurrentMouseState.LeftButton == ButtonState.Pressed;
    }

    public bool MouseReleased() {
      return OldMouseState.LeftButton == ButtonState.Pressed
             && CurrentMouseState.LeftButton == ButtonState.Released;
    }

    public Vector2 MousePosition() {
      return new Vector2(CurrentMouseState.X, CurrentMouseState.Y);
    }

    public bool MouseIsOnClient() {
      return CurrentMouseState.X >= 0 && CurrentMouseState.Y >= 0
             && CurrentMouseState.X < Game.Window.ClientBounds.Width
             && CurrentMouseState.Y < Game.Window.ClientBounds.Height;
    }

    KeyboardState CurrentKeyboardState;
    KeyboardState OldKeyboardState;
    MouseState CurrentMouseState;
    MouseState OldMouseState;
  }
}
