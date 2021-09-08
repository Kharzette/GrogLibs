#define BATCH_SIZE	4	//should match ShadowKeeper constant

float3	mLightPositions[BATCH_SIZE];

float4x4	mWorld;				//world mat for shadowcaster

float4x4	mLightViews[BATCH_SIZE * 6];	//cubemap face viewmats
float4x4	mLightProjs[BATCH_SIZE * 6];	//cubemap face projections

#include "Types.fxh"


VVPosTex04	WorldPosVS(VPos input, uint InstanceID : SV_InstanceID)
{
	float4	vertPos			=float4(input.Position, 1);

	int	batch		=InstanceID / BATCH_SIZE;
	int	cubeFace	=InstanceID % BATCH_SIZE;
	int	matIdx		=(batch * 6) + cubeFace;

	float4x4	vp	=mul(mLightViews[matIdx], mLightProjs[matIdx]);

	float4	worldVertPos	=mul(vertPos, mWorld);

	VVPosTex04	output;

	output.Position			=mul(worldVertPos, vp);
	output.TexCoord0.xyz	=worldVertPos.xyz;
	output.TexCoord0.w		=InstanceID;

	return	output;
}


//just a passthru that sets the cube face
[maxvertexcount(3)]
void	ShadowGS(triangle VVPosTex04 input[3], inout TriangleStream<VVPosTex04RTAI> ret)
{
	VVPosTex04RTAI	outStuff;

	uint	InstanceID	=input[0].TexCoord0.w;

	uint	batch		=InstanceID / BATCH_SIZE;
	uint	cubeFace	=InstanceID % BATCH_SIZE;
	uint	matIdx		=(batch * 6) + cubeFace;

	[unroll(3)]
	for(int i=0;i < 3;i++)
	{
		outStuff.Position	=input[i].Position;
		outStuff.TexCoord0	=input[i].TexCoord0;
		outStuff.CubeFace	=cubeFace;

		ret.Append(outStuff);
	}
}

struct	CubeTarg
{
	float4	mNegXFace : SV_Target0;
	float4	mNegYFace : SV_Target1;
	float4	mNegZFace : SV_Target2;
	float4	mPosXFace : SV_Target3;
	float4	mPosYFace : SV_Target4;
	float4	mPosZFace : SV_Target5;
};


float4	ShadowPS(VVPosTex04RTAI input) : SV_Target
{
	uint	instID	=input.TexCoord0.w;

	uint	batch		=instID / 6;
	uint	cubeFace	=instID % 6;

	//distance to light
	float	dist	=distance(mLightPositions[batch], input.TexCoord0.xyz);

	float4	distVal;

	switch(batch)
	{
		case	0:
			distVal	=float4(dist, 0, 0, 0);
			break;
		case	1:
			distVal	=float4(0, dist, 0, 0);
			break;
		case	2:
			distVal	=float4(0, 0, dist, 0);
			break;
		case	3:
			distVal	=float4(0, 0, 0, dist);
			break;
	}
	return	distVal;
/*
	switch(cubeFace)
	{
		case	0:
			ret.mNegXFace	=distVal;
			break;
		case	1:
			ret.mNegYFace	=distVal;
			break;
		case	2:
			ret.mNegZFace	=distVal;
			break;
		case	3:
			ret.mPosXFace	=distVal;
			break;
		case	4:
			ret.mPosYFace	=distVal;
			break;
		case	5:
			ret.mPosZFace	=distVal;
			break;
	}

	return	ret;*/
}

technique10	ShadowBatch
{
	pass	P0
	{
		VertexShader	=compile vs_5_0 WorldPosVS();
		GeometryShader	=compile gs_5_0 ShadowGS();
		PixelShader		=compile ps_5_0	ShadowPS();
	}
}