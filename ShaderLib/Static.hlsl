struct VPosNormTex0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half2	TexCoord0	: TEXCOORD0;
};

struct VVPosTex04Tex14
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
};

SamplerState LinearWrap
{
	Filter		=MIN_MAG_MIP_LINEAR;
	AddressU	=Wrap;
	AddressV	=Wrap;
};

//constants
#define	MAX_BONES				55
#define	PI_OVER_FOUR			0.7853981634f
#define	PI_OVER_TWO				1.5707963268f
#define MAX_HALF				65504
#define	OUTLINE_ALPHA_THRESHOLD	0.15

cbuffer PerObject : register(b0)
{
	float4x4	mWorld;
	float4		mSolidColour;
	float4		mSpecColor;

	//These are considered directional (no falloff)
	float4	mLightColor0;		//trilights need 3 colors
	float4	mLightColor1;		//trilights need 3 colors
	float4	mLightColor2;		//trilights need 3 colors

	float3	mLightDirection;
	float	mSpecPower;
}

cbuffer	PerFrame : register(b1)
{
	float4x4	mView;
	float4x4	mLightViewProj;	//for shadowing
	float3		mEyePos;
	uint		mPadding;
}

cbuffer ChangeLess : register(b2)
{
	float4x4	mProjection;
}

//texture layers used on the surface
Texture2D	mTexture0 : register(t0);
Texture2D	mTexture1 : register(t1);


//compute the 3 light effects on the vert
//see http://home.comcast.net/~tom_forsyth/blog.wiki.html
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

float3 ComputeGoodSpecular(float3 wpos, float3 lightDir, float3 pnorm, float3 lightVal, float4 fillLight)
{
	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, pnorm));
	float	ndoth	=saturate(dot(halfVec, pnorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	//fresnel stuff
	float	base		=1.0f - dot(halfVec, lightDir);
	float	exponential	=pow(base, 5.0f);
	float3	fresTerm	=mSpecColor + (1.0f - mSpecColor) * exponential;

	//vis stuff
	float	alpha	=1.0f / (sqrt(PI_OVER_FOUR * mSpecPower + PI_OVER_TWO));
	float	visTerm	=(lightVal * (1.0f - alpha) + alpha) *
				(ndotv * (1.0f - alpha) + alpha);

	visTerm	=1.0f / visTerm;

	float3	specular	=specTerm * lightVal * fresTerm * visTerm * fillLight;

	return	specular;
}

float3 ComputeCheapSpecular(float3 wpos, float3 lightDir, float3 pnorm, float3 lightVal, float4 fillLight)
{
	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, pnorm));
	float	ndoth	=saturate(dot(halfVec, pnorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	float3	specular	=specTerm * lightVal * fillLight;

	return	specular;
}

//worldpos and normal
VVPosTex04Tex14 WNormWPosTexVS(VPosNormTex0 input)
{
	VVPosTex04Tex14	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position			=mul(float4(input.Position, 1), wvp);
	output.TexCoord0.xyz	=mul(input.Normal.xyz, mWorld);
	output.TexCoord1.xyz	=mul(input.Position, mWorld);
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.w		=input.TexCoord0.y;
	
	//return the output structure
	return	output;
}

//Texture 0, trilight, modulate solid, and specular
float4 TriTex0SpecPS(VVPosTex04Tex14 input) : SV_Target
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

//	float4	texColor	=mTexture0.Sample(LinearWrap, tex);
	float4	texColor	=float4(1,1,1,1);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	wpos	=input.TexCoord1.xyz;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

#if defined(SM2)
	float3	specular	=ComputeCheapSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#else
	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#endif

	float3	litColor	=texColor.xyz * triLight;

	specular	=saturate((specular + litColor.xyz) * mSolidColour.xyz);

	return	float4(specular, texColor.w);
}
