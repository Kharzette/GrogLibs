//common functions used by most shaders
#ifndef _COMMONFUNCTIONSFXH
#define _COMMONFUNCTIONSFXH

//constants
#define	MAX_BONES	50

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;
shared float3	mEyePos;

//nearby dynamic lights?
shared float3		mLight0Position;
shared float3		mLight0Color;
shared float		mLightRange;
shared float		mLightFalloffRange;	//under this light at full strength

//outline / cell related
float	mCellThresholds[4] = { 0.6, 0.4, 0.25, 0.1 };
float	mCellBrightnessLevels[5] = { 1.0f, 0.7f, 0.5f, 0.2f, 0.05f };

#include "Types.fxh"


//does the math to get a normal from a sampled
//normal map to a proper normal useful for lighting
float3 ComputeNormalFromMap(float4 sampleNorm, float3 tan, float3 biTan, float3 surfNorm)
{
	//convert normal from 0 to 1 to -1 to 1
	sampleNorm	=2.0 * sampleNorm - float4(1.0, 1.0, 1.0, 1.0);

	float3x3	tbn	=float3x3(
					normalize(tan),
					normalize(biTan),
					normalize(surfNorm));
	
	//I borrowed a bunch of my math from GL samples thus
	//this is needed to get things back into XNA Land
	tbn	=transpose(tbn);

	//rotate normal into worldspace
	sampleNorm.xyz	=mul(tbn, sampleNorm.xyz);
	sampleNorm.xyz	=normalize(sampleNorm.xyz);

	return	sampleNorm.xyz;
}


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
float3 ComputeTrilight(float3 normal, float3 lightDir, float3 c0, float3 c1, float3 c2)
{
    float3	totalLight;
	float	LdotN	=dot(normal, lightDir);
	
	//trilight
	totalLight	=(c0 * max(0, LdotN))
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

	output.Position		=skinny.Position;	
	output.Color.xyz	=ComputeTrilight(skinny.Normal, lightDir, c0, c1, c2);
	output.Color.w		=1.0;
	
	return	output;
}


//snaps a color to a cellish range
float CalcCellLight(float3 lightVal)
{
	float	light;

	float	d	=lightVal.x + lightVal.y + lightVal.z;

	d	*=0.33;

	if(d > mCellThresholds[0])
	{
		light	=mCellBrightnessLevels[0];
	}
	else if(d > mCellThresholds[1])
	{
		light	=mCellBrightnessLevels[1];
	}
	else if(d > mCellThresholds[2])
	{
		light	=mCellBrightnessLevels[2];
	}
	else if(d > mCellThresholds[3])
	{
		light	=mCellBrightnessLevels[3];
	}
	else
	{
		light	=mCellBrightnessLevels[4];
	}

	return	light;
}


//snaps a color to a cellish range
float3 CalcCellColor(float3 colVal)
{
	float3	col;

	if(colVal.x > mCellThresholds[0])
	{
		col.x	=mCellBrightnessLevels[0];
	}
	else if(colVal.x > mCellThresholds[1])
	{
		col.x	=mCellBrightnessLevels[1];
	}
	else if(colVal.x > mCellThresholds[2])
	{
		col.x	=mCellBrightnessLevels[2];
	}
	else if(colVal.x > mCellThresholds[3])
	{
		col.x	=mCellBrightnessLevels[3];
	}
	else
	{
		col.x	=mCellBrightnessLevels[4];
	}

	if(colVal.y > mCellThresholds[0])
	{
		col.y	=mCellBrightnessLevels[0];
	}
	else if(colVal.y > mCellThresholds[1])
	{
		col.y	=mCellBrightnessLevels[1];
	}
	else if(colVal.y > mCellThresholds[2])
	{
		col.y	=mCellBrightnessLevels[2];
	}
	else if(colVal.y > mCellThresholds[3])
	{
		col.y	=mCellBrightnessLevels[3];
	}
	else
	{
		col.y	=mCellBrightnessLevels[4];
	}

	if(colVal.z > mCellThresholds[0])
	{
		col.z	=mCellBrightnessLevels[0];
	}
	else if(colVal.z > mCellThresholds[1])
	{
		col.z	=mCellBrightnessLevels[1];
	}
	else if(colVal.z > mCellThresholds[2])
	{
		col.z	=mCellBrightnessLevels[2];
	}
	else if(colVal.z > mCellThresholds[3])
	{
		col.z	=mCellBrightnessLevels[3];
	}
	else
	{
		col.z	=mCellBrightnessLevels[4];
	}

	return	col;
}
#endif	//_COMMONFUNCTIONSFXH