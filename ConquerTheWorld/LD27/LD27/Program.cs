namespace ConquerTheWorld {
  internal static class Program {
    static void Main(string[] args) {
      using (var game = new GameMain()) {
        game.Run();
      }
    }
  }
}

