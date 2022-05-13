#define BATCH_SIZE	24		//should match ShadowKeeper constant
#define BIGNUM		65504	//big depth val

float3	mLightPositions[BATCH_SIZE];

float4x4	mWorld;				//world mat for shadowcaster

float4x4	mLightViews[BATCH_SIZE * 6];	//cubemap face viewmats
float4x4	mLightProjs[BATCH_SIZE * 6];	//cubemap face projections

#include "Types.fxh"


VVPosTex04	WorldPosVS(VPos input, uint InstanceID : SV_InstanceID)
{
	float4	vertPos			=float4(input.Position, 1);

	float4x4	vp	=mul(mLightViews[InstanceID], mLightProjs[InstanceID]);

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
	uint	cubeFace	=InstanceID % BATCH_SIZE;

	[unroll(3)]
	for(int i=0;i < 3;i++)
	{
		outStuff.Position	=input[i].Position;
		outStuff.TexCoord0	=input[i].TexCoord0;
		outStuff.CubeFace	=cubeFace;

		ret.Append(outStuff);
	}
}

struct	BatchTarg
{
	float4	mCube0 : SV_Target0;
	float4	mCube1 : SV_Target1;
	float4	mCube2 : SV_Target2;
	float4	mCube3 : SV_Target3;
	float4	mCube4 : SV_Target4;
	float4	mCube5 : SV_Target5;
	float4	mCube6 : SV_Target6;
	float4	mCube7 : SV_Target7;
};


BatchTarg	ShadowPS(VVPosTex04RTAI input)
{
	//initialize this to bigval so min blend works
	BatchTarg	ret	=(BatchTarg)BIGNUM;

	uint	instID	=input.TexCoord0.w;

	uint	light		=instID / 6;
	uint	cube		=light / 3;		//3 lights per cube
	uint	ccomp		=light % 3;		//which color component to use?

	//distance to light
	float	dist	=distance(mLightPositions[light], input.TexCoord0.xyz);

	float4	distVal;

	switch(ccomp)
	{
		case	0:
			distVal	=float4(dist, BIGNUM, BIGNUM, BIGNUM);
			break;
		case	1:
			distVal	=float4(BIGNUM, dist, BIGNUM, BIGNUM);
			break;
		case	2:
			distVal	=float4(BIGNUM, BIGNUM, dist, BIGNUM);
			break;
	}

	switch(cube)
	{
		case	0:
			ret.mCube0	=distVal;
			break;
		case	1:
			ret.mCube1	=distVal;
			break;
		case	2:
			ret.mCube2	=distVal;
			break;
		case	3:
			ret.mCube3	=distVal;
			break;
		case	4:
			ret.mCube4	=distVal;
			break;
		case	5:
			ret.mCube5	=distVal;
			break;
		case	6:
			ret.mCube6	=distVal;
			break;
		case	7:
			ret.mCube7	=distVal;
			break;
	}

	return	ret;
}

technique10	ShadowBatch
{
	pass	P0
	{
		VertexShader	=compile vs_5_0 WorldPosVS();
		GeometryShader	=compile gs_5_0 ShadowGS();
		PixelShader		=compile ps_5_0	ShadowPS();

		SetBlendState(MultiChannelDepth, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}