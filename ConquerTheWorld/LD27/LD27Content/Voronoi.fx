const static int NUM_REGIONS = 10;
float2 VoronoiPoints[NUM_REGIONS];
float4 VoronoiColors[NUM_REGIONS];
float2 Dimensions;

struct VertexShaderInputOutput {
  float4 ScreenSpacePosition : POSITION0;
  float2 PixelSpacePosition : TEXCOORD0;
};

VertexShaderInputOutput VertexShaderFunction(VertexShaderInputOutput input) {
  VertexShaderInputOutput output = input;
  output.PixelSpacePosition = input.PixelSpacePosition * Dimensions;
  return output;
}

float4 PixelShaderFunction(VertexShaderInputOutput input) : COLOR0 {
  float minimumDistance = 1e6;
  float secondMinimumDistance = 1e6;
  float4 color = float4(0, 0, 0, 0);
  for (int i = 0; i < NUM_REGIONS; ++i) {
    float distance = length(input.PixelSpacePosition - VoronoiPoints[i]);
    if (distance < minimumDistance) {
      secondMinimumDistance = minimumDistance;
      minimumDistance = distance;
      color = VoronoiColors[i];
    } else if (distance < secondMinimumDistance) {
      secondMinimumDistance = distance;
    }
  }
  float totalDistance = (minimumDistance + secondMinimumDistance) * .75;
  return lerp(color, float4(0, 0, 0, 1), minimumDistance / totalDistance);
}

technique {
    pass {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
