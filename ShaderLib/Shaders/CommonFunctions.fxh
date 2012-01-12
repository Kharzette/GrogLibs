//common functions used by most shaders
#ifndef _COMMONFUNCTIONSFXH
#define _COMMONFUNCTIONSFXH

//constants
#define	MAX_BONES	50

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;

//nearby dynamic lights?
shared float3		mLight0Position;
shared float3		mLight0Color;
shared float		mLightRange;
shared float		mLightFalloffRange;	//under this light at full strength

#include "Types.fxh"


float3 ComputeLight(float3 worldPos, float3 lightPos, float3 normal)
{
	float3	col		=float3(0, 0, 0);
	float	dist	=distance(worldPos, lightPos);
	if(dist < mLightRange)
	{
		float3	lightDirection	=normalize(lightPos - worldPos);
		float3	worldNormal		=mul(normal, mWorld);
		float	ndl				=dot(worldNormal, lightDirection);
		
		//distance falloff
		if(dist > mLightFalloffRange)
		{
			ndl	*=(1 - ((dist - mLightFalloffRange) / (mLightRange - mLightFalloffRange)));
		}		
		col	=mLight0Color * ndl;
	}
	return	col;
}


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


VPosNorm ComputeSkin(VPosNormBone input, float4x4 bones[MAX_BONES], float4x4 bindPose)
{
	VPosNorm	output;
	
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
	output.Normal	=mul(worldNormal, mWorld);

	return	output;
}


//compute the position and color of a skinned vert
VPosCol0 ComputeSkinTrilight(VPosNormBone input, float4x4 bones[MAX_BONES],
							 float4x4 bindPose, float3 lightDir,
							 float4 c0, float4 c1, float4 c2)
{
	VPosCol0	output;
	VPosNorm	skinny	=ComputeSkin(input, bones, bindPose);

	output.Position	=skinny.Position;	
	output.Color	=ComputeTrilight(skinny.Normal, lightDir, c0, c1, c2);
	
	return	output;
}
#endif	//_COMMONFUNCTIONSFXH