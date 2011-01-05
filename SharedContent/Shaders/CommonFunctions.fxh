//common functions used by most shaders
#ifndef _COMMONFUNCTIONSFXH
#define _COMMONFUNCTIONSFXH

//constants
#define	MAX_BONES	50

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;

#include "Types.fxh"


//look up the skin transform
float4x4 GetSkinXForm(float4 bnIdxs, float4 bnWeights, float4x4 bones[MAX_BONES])
{
	float4x4 skinTransform	=0;
	skinTransform	+=bones[bnIdxs.x] * bnWeights.x;
	skinTransform	+=bones[bnIdxs.y] * bnWeights.y;
	skinTransform	+=bones[bnIdxs.z] * bnWeights.z;
	skinTransform	+=bones[bnIdxs.w] * bnWeights.w;
	
	return	skinTransform;
}


//compute the 3 light effects on the vert
float4 ComputeTrilight(float3 normal, float3 lightDir, float4 c0, float4 c1, float4 c2)
{
    float4	totalLight	=float4(0,0,0,1);
	float	LdotN		=dot(normal, lightDir);
	
	//trilight
	totalLight	+=(c0 * max(0, LdotN))
		+ (c1 * (1 - abs(LdotN)))
		+ (c2 * max(0, -LdotN));
		
	return	totalLight;
}


//compute the position and color of a skinned vert
VPosCol0 ComputeSkinTrilight(VPosNormBone input, float4x4 bones[MAX_BONES],
							 float4x4 bindPose, float3 lightDir,
							 float4 c0, float4 c1, float4 c2)
{
	VPosCol0	output;
	
	float4	vertPos	=mul(input.Position, bindPose);
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//do the bone influences
	float4x4 skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, bones);
	
	//xform the vert to the character's boney pos
	vertPos	=mul(vertPos, skinTransform);
	
	//transform the input position to the output
	output.Position	=mul(vertPos, wvp);

	//skin transform the normal
	float3	worldNormal	=mul(input.Normal, skinTransform);
	
	//world transform the normal
	worldNormal	=mul(worldNormal, mWorld);
	
	output.Color	=ComputeTrilight(worldNormal, lightDir, c0, c1, c2);
	
	return	output;
}
#endif	//_COMMONFUNCTIONSFXH