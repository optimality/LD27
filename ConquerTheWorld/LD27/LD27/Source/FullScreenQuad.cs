using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerTheWorld {
  public class FullScreenQuad {
    public FullScreenQuad(GraphicsDevice graphics) {
      VertexBuffer = new VertexBuffer(
          graphics, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
      VertexBuffer.SetData(
          new[] {
              new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
              new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
              new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
              new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0))
          });
    }

    public void Draw(GraphicsDevice graphics) {
      graphics.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
    }

    public void Prepare(GraphicsDevice graphics) {
      graphics.SetVertexBuffer(VertexBuffer);
    }

    readonly VertexBuffer VertexBuffer;
  }
}
