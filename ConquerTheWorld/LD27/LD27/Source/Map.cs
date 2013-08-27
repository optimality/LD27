using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerTheWorld {
  public class Map : DrawableGameComponent {
    public Map(Game game, int numRegions, Viewport viewport, int aiTickTime) : base(game) {
      Viewport = viewport;
      GenerateRegions(numRegions);
      AITickTimeMS = aiTickTime;
    }

    public int NumPlayerOwnedRegions {
      get {
        int numPlayerOwnedRegions = 0;
        foreach (Region region in Regions) {
          if (region.PlayerOwned) {
            numPlayerOwnedRegions += 1;
          }
        }
        return numPlayerOwnedRegions;
      }
    }

    public override void Update(GameTime gameTime) {
      base.Update(gameTime);
      ProcessInput();
      TimeSinceLastTickMS += gameTime.ElapsedGameTime.Milliseconds;
      TimeSinceLastAITickMS += gameTime.ElapsedGameTime.Milliseconds;
      if (TimeSinceLastTickMS > TICK_TIME_MS) {
        UpdateGameState();
        TimeSinceLastTickMS = 0;
      }
      if (TimeSinceLastAITickMS > AITickTimeMS) {
        RunAI(false);
        TimeSinceLastAITickMS = 0;
      }
    }

    void UpdateGameState() {
      foreach (Region region in Regions) {
        bool oldOwner = region.PlayerOwned;
        region.Update();
        bool newOwner = region.PlayerOwned;
        if (newOwner != oldOwner) {
          if (newOwner) {
            ConquerSound.Play();
          } else {
            LossSound.Play();
          }
        }
      }
    }

    void RunAI(bool player) {
      if (NumPlayerOwnedRegions == Regions.Length || NumPlayerOwnedRegions == 0) {
        return;
      }
      Region bestSourceRegion = null;
      AdjacentRegion bestTargetRegion = null;
      int bestScore = int.MinValue;

      foreach (Region sourceRegion in Regions) {
        if (sourceRegion.PlayerOwned != player) {
          continue;
        }

        AdjacentRegion bestRegion = null;
        int bestRegionScore = int.MinValue;
        foreach (AdjacentRegion adjacentRegion in sourceRegion.Adjacent) {
          int numAttackers = 0;
          int numDefenders = 0;
          foreach (Region region in Regions) {
            // Don't count self
            if (region == sourceRegion) {
              continue;
            }
            if (region.Target != null && region.Target.Region == adjacentRegion.Region) {
              if (region.PlayerOwned == adjacentRegion.Region.PlayerOwned) {
                ++numDefenders;
              } else {
                ++numAttackers;
              }
            }
          }
          int score;
          if (adjacentRegion.Region.PlayerOwned == player) {
            // Defend!
            if (numAttackers == numDefenders + 1) {
              score = 100;
            } else if (numAttackers < numDefenders - 1) {
              score = -100;
            } else {
              score = 0;
            }
          } else {
            // Start fights.
            if (numAttackers == numDefenders) {
              score = 1000;
            } else if (numAttackers - 1 > numDefenders) {
              score = -10; // Reallocate attackers.
            } else {
              score = 10;
            }
          }
          if (score > bestRegionScore) {
            bestRegionScore = score;
            bestRegion = adjacentRegion;
          }
        }
        if (sourceRegion.Target != bestRegion && bestRegionScore > bestScore) {
          bestScore = bestRegionScore;
          bestSourceRegion = sourceRegion;
          bestTargetRegion = bestRegion;
        }
      }

      if (bestSourceRegion != null) {
        bestSourceRegion.Target = bestTargetRegion;
      }
    }

    void ProcessInput() {
      var input = (InputManager) Game.Services.GetService(typeof(InputManager));
      if (input.MouseReleased() && input.MouseIsOnClient()) {
        RegionSelected(FindClosestRegion(input.MousePosition()));
      }
    }

    public override void Draw(GameTime gameTime) {
      base.Draw(gameTime);
      Viewport oldViewport = GraphicsDevice.Viewport;
      GraphicsDevice.Viewport = Viewport;
      DrawVoronoi();
      DrawArrows();
      GraphicsDevice.Viewport = oldViewport;
    }

    const int MIN_DISTANCE_BETWEEN_POINTS_SQUARED = 10000;
    const int BORDER_WIDTH = 50;
    const float TICK_TIME_MS = 100f;
    readonly int AITickTimeMS;
    float TimeSinceLastTickMS;
    float TimeSinceLastAITickMS;
    SoundEffect ConquerSound;
    SoundEffect LossSound;

    Viewport Viewport { get; set; }
    Region[] Regions { get; set; }
    Region SelectedRegion { get; set; }
    Vector2[] Centers {
      get {
        var centers = new Vector2[Regions.Length];
        for (int i = 0; i < Regions.Length; ++i) {
          centers[i] = Regions[i].Center;
        }
        return centers;
      }
    }

    Vector4[] Colors {
      get {
        var colors = new Vector4[Regions.Length];
        for (int i = 0; i < Regions.Length; ++i) {
          colors[i] = Regions[i].Color.ToVector4();
        }
        return colors;
      }
    }

    Texture2D ArrowTexture;
    Effect VoronoiEffect;
    FullScreenQuad FullScreenQuad;

    protected override void LoadContent() {
      base.LoadContent();
      VoronoiEffect = Game.Content.Load<Effect>("Voronoi");
      ArrowTexture = Game.Content.Load<Texture2D>("Arrow");
      FullScreenQuad = new FullScreenQuad(GraphicsDevice);
      ConquerSound = Game.Content.Load<SoundEffect>("Conquer");
      LossSound = Game.Content.Load<SoundEffect>("Loss");
    }

    int FindClosestRegion(Vector2 position) {
      float minDistanceSquared = float.MaxValue;
      int closestRegion = -1;
      for (int i = 0; i < Regions.Length; ++i) {
        float distance = Vector2.DistanceSquared(Regions[i].Center, position);
        if (distance < minDistanceSquared) {
          closestRegion = i;
          minDistanceSquared = distance;
        }
      }
      return closestRegion;
    }

    void RegionSelected(int region) {
      Region targetRegion = Regions[region];
      // If they click the current region, disable it.
      if (SelectedRegion == targetRegion) {
        Debug.Assert(SelectedRegion != null, "SelectedRegion != null");
        SelectedRegion.Selected = false;
        SelectedRegion = null;
      } else if (SelectedRegion == null) {
        if (!targetRegion.PlayerOwned) {
          return;
        }
        SelectedRegion = targetRegion;
        SelectedRegion.Selected = true;
      } else {
        AdjacentRegion adjacentRegion = SelectedRegion.GetAdjacent(targetRegion);
        if (adjacentRegion != null) {
          SelectedRegion.Target = adjacentRegion;
          SelectedRegion.Selected = false;
          SelectedRegion = null;
        }
      }
    }

    void DrawArrows() {
      foreach (Region region in Regions) {
        if (region.Target != null) {
          DrawArrow(region, region.Target);
        }
      }
    }

    void DrawArrow(Region from, AdjacentRegion to) {
      Vector2 direction = to.Region.Center - from.Center;
      var spriteBatch = (SpriteBatch) Game.Services.GetService(typeof(SpriteBatch));
      spriteBatch.Begin();
      float angle = (float) Math.Atan2(direction.Y, direction.X) + (float) Math.PI / 2;
      var scale = new Vector2(1, 1);
      var origin = new Vector2(ArrowTexture.Width * .6f, ArrowTexture.Height * .5f);
      spriteBatch.Draw(
          ArrowTexture, to.Position, null, from.Color, angle, origin, scale, SpriteEffects.None, 0);
      spriteBatch.End();
    }

    /*
    void DrawRegionNumbers() {
      var font = (SpriteFont) Game.Services.GetService(typeof(SpriteFont));
      var spriteBatch = (SpriteBatch) Game.Services.GetService(typeof(SpriteBatch));
      spriteBatch.Begin();
      for (int i = 0; i < Centers.Length; ++i) {
        string text = string.Format("{0}", i);
        spriteBatch.DrawString(font, text, Centers[i], Color.White);
      }
      spriteBatch.End();
    }
     */

    void DrawVoronoi() {
      VoronoiEffect.Parameters["VoronoiPoints"].SetValue(Centers);
      VoronoiEffect.Parameters["VoronoiColors"].SetValue(Colors);
      VoronoiEffect.Parameters["Dimensions"].SetValue(new Vector2(Viewport.Width, Viewport.Height));
      VoronoiEffect.CurrentTechnique.Passes[0].Apply();

      FullScreenQuad.Prepare(GraphicsDevice);
      FullScreenQuad.Draw(GraphicsDevice);
    }

    void GenerateRegions(int numRegions) {
      var random = (Random)Game.Services.GetService(typeof(Random));
      Regions = new Region[numRegions];
      for (int i = 0; i < numRegions; ++i) {
        Vector2 center;
        int testedCenters = 0;
        do {
          center = GenerateCandidateCenter(Viewport.Width, Viewport.Height, random);
          testedCenters += 1;
        } while (!PointsFarEnoughApart(center, Regions) && testedCenters < 100);
        if (testedCenters >= 100) {
          throw new Exception("Could not find a reasonable center point!");
        }
        Regions[i] = new Region {
            Center = center,
            PlayerOwned = false,
            Selected = false,
            Health = 10,
        };
      }
      foreach (Region region in Regions) {
        region.Adjacent = GetRegionsAdjacentTo(region);
      }
      for (int i = 0; i < numRegions / 2; ++i) {
        Regions[i].PlayerOwned = true;
      }
    }

    static Vector2 GenerateCandidateCenter(int width, int height, Random random) {
      return new Vector2(
          random.Next(BORDER_WIDTH, width - BORDER_WIDTH),
          random.Next(BORDER_WIDTH, height - BORDER_WIDTH));
    }

    static bool PointsFarEnoughApart(Vector2 center, IEnumerable<Region> otherRegions) {
      foreach (Region otherRegion in otherRegions) {
        if (otherRegion != null
            && Vector2.DistanceSquared(center, otherRegion.Center)
            < MIN_DISTANCE_BETWEEN_POINTS_SQUARED) {
          return false;
        }
      }
      return true;
    }

    IList<AdjacentRegion> GetRegionsAdjacentTo(Region region) {
      IDictionary<Region, IList<Vector2>> adjacent = new Dictionary<Region, IList<Vector2>>();
      for (float angle = 0; angle < 2 * Math.PI; angle += .1f) {
        Vector2 ray = Vector2.Transform(
            Vector2.UnitX, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle));
        ray.Normalize();
        Tuple<Region, Vector2> adjacentRegionAndPoint = GetFirstRegionInDirection(region, ray);
        if (adjacentRegionAndPoint != null) {
          IList<Vector2> adjacentPoints;
          if (!adjacent.TryGetValue(adjacentRegionAndPoint.Item1, out adjacentPoints)) {
            adjacentPoints = new List<Vector2>();
            adjacent.Add(adjacentRegionAndPoint.Item1, adjacentPoints);
          }
          Debug.Assert(adjacentPoints != null, "adjacentPoints != null");
          adjacentPoints.Add(adjacentRegionAndPoint.Item2);
        }
      }
      IList<AdjacentRegion> adjacentRegions = new List<AdjacentRegion>();
      foreach (KeyValuePair<Region, IList<Vector2>> regionPointsPair in adjacent) {
        IList<Vector2> adjacentPoints = regionPointsPair.Value;
        Vector2 adjacencyPoint = Vector2.Zero;
        foreach (Vector2 point in adjacentPoints) {
          adjacencyPoint += point;
        }
        adjacencyPoint /= regionPointsPair.Value.Count;

        adjacentRegions.Add(
            new AdjacentRegion {
                Position = adjacencyPoint,
                Region = regionPointsPair.Key
            });
      }
      return adjacentRegions;
    }

    Tuple<Region, Vector2> GetFirstRegionInDirection(Region region, Vector2 ray) {
      Vector2 samplePoint = region.Center;
      while (samplePoint.X > 0 && samplePoint.Y > 0 && samplePoint.X < Viewport.Width
             && samplePoint.Y < Viewport.Height) {
        samplePoint += ray;
        Region closestRegion = Regions[FindClosestRegion(samplePoint)];
        if (closestRegion != region) {
          return new Tuple<Region, Vector2>(closestRegion, samplePoint);
        }
      }
      return null;
    }
  }

  public class Region {
    const int MAX_HEALTH = 10;
    public Vector2 Center { get; set; }
    public bool PlayerOwned { get; set; }
    public bool Selected { get; set; }
    public AdjacentRegion Target { get; set; }
    public IList<AdjacentRegion> Adjacent { get; set; }
    public int Health { get; set; }

    public Color Color {
      get {
        Color color = PlayerOwned ? Color.Blue : Color.Red;
        if (!Selected) {
          color *= .5f;
        }
        color *= Health / (float)MAX_HEALTH;
        return color;
      }
    }

    public AdjacentRegion GetAdjacent(Region selectedRegion) {
      foreach (AdjacentRegion adjacent in Adjacent) {
        if (adjacent.Region == selectedRegion) {
          return adjacent;
        }
      }
      return null;
    }

    public void Update() {
      int supportingRegions = 0;
      int attackingRegions = 0;
      foreach (AdjacentRegion adjacentRegion in Adjacent) {
        AdjacentRegion target = adjacentRegion.Region.Target;
        if (target != null && target.Region == this) {
          if (adjacentRegion.Region.PlayerOwned == PlayerOwned) {
            supportingRegions += 1;
          } else {
            attackingRegions += 1;
          }
        }
      }
      int healthDelta = supportingRegions - attackingRegions;
      Health += healthDelta;
      if (Health > MAX_HEALTH) {
        Health = MAX_HEALTH;
      }
      if (Health <= 0) {
        PlayerOwned = !PlayerOwned;
        Health = MAX_HEALTH;
        Target = null;
      }
    }
  }

  public class AdjacentRegion {
    public Region Region { get; set; }
    public Vector2 Position { get; set; }
  }
}
