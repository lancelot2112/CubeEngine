float4x4 World;
float4x4 View;
float4x4 Projection;
float FogStartRadius = 200;
float FogRange = 150;
float4 FogColor = {0.96,0.96,0.96,1};

//sampler NormalMap;
//sampler Texture;

static const int num_of_lights = 10;
static const float inv_255 = 1.0/255.0;
float3 SkyLightDir;
float3 fLightColor[num_of_lights];
float3 fLightPos[num_of_lights];

struct VertexShaderInput
{
    float3 position : POSITION0;
	byte4 normal : NORMAL0;
	float2 uv : TEXCOORD0;
	byte4 color : COLOR0;
	byte4 light : COLOR1;
	short2 brightness : COLOR2;
};

struct VertexShaderOutput
{
    float4 position : POSITION0;
	float4 color : COLOR0;
	float4 light : COLOR1;
	float2 brightness : COLOR2;
	float3 worldPos : COLOR3;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float fog : FOG;

};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPos = mul(float4(input.position,1.0), World);
    float4 viewPosition = mul(worldPos, View);
    output.position = mul(viewPosition, Projection);
	output.worldPos = worldPos;

	output.uv = input.uv;
	output.normal = float4(input.normal.xyzw);
	output.color = float4(input.color.xyz*inv_255,input.color.w);
	output.light = float4(input.light.xyz*inv_255,input.light.w);
	output.brightness = float2(input.brightness/21.4 + 0.3);
	float radius = distance(output.position.xz,float2(0,0));
	output.fog = saturate((radius-FogStartRadius)/FogRange);
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 tmpColor = input.color;

	//float3 albedo = tex2D(Texture, input.uv);

	//Fetch the normal from the texture
	//Need to bias the result to get it in the range of -1 to 1
	//float3 tmpNormal = 2.0 * (tex2D(NormalMap, input.uv)-0.5);

	//Renormalize our resulting inputs and get binormal
	//float3 tangent  = normalize(input.tangent);
	float3 normal = normalize(input.normal);
	//float3 binormal = cross(tangent,normal);

	//Compute Matrix inverse
	//float3x3 matInverse = transpose(float3x3(tangent,binormal,normal));

	//Bring normal into world space
	//normal = mul(mul(tmpNormal,matInverse),World);
    
	for(int i = 0; i < num_of_lights; i++)
	{
		//Compute the distance to light
		float dist = length(input.worldPos - fLightPos[i]);
		//...and direction
		float3 direction = normalize(input.worldPos - fLightPos[i]);
		//...and attenuation
		float att = 1/dist;
		//Add light contribution
		tmpColor.xyz += fLightColor[i]*att*dot(normal,-direction);
	}

	float3 sun = tmpColor.xyz * input.brightness.y * (dot(SkyLightDir, normal));
	tmpColor.xyz = tmpColor.xyz * input.light.xyz * input.light.w + sun;
	tmpColor = lerp(tmpColor,FogColor,input.fog);
	return tmpColor;
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
