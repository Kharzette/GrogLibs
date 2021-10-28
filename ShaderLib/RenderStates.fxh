#ifndef _RENDERSTATESFXH
#define _RENDERSTATESFXH

//commonly used renderstates
BlendState AlphaBlending
{
	AlphaToCoverageEnable		=FALSE;
	BlendEnable[0]				=TRUE;
	BlendEnable[1]				=TRUE;
	BlendEnable[2]				=TRUE;
	BlendEnable[3]				=TRUE;
	BlendEnable[4]				=TRUE;
	BlendEnable[5]				=TRUE;
	BlendEnable[6]				=TRUE;
	BlendEnable[7]				=TRUE;
	SrcBlend					=ONE;
	DestBlend					=INV_SRC_ALPHA;
	BlendOp						=ADD;
	SrcBlendAlpha				=ZERO;
	DestBlendAlpha				=ZERO;
	BlendOpAlpha				=ADD;
	RenderTargetWriteMask[0]	=0x0F;
};

BlendState MultiChannelDepth
{
	AlphaToCoverageEnable		=FALSE;
	BlendEnable[0]				=TRUE;
	BlendEnable[1]				=TRUE;
	BlendEnable[2]				=TRUE;
	BlendEnable[3]				=TRUE;
	BlendEnable[4]				=TRUE;
	BlendEnable[5]				=TRUE;
	BlendEnable[6]				=TRUE;
	BlendEnable[7]				=TRUE;
	SrcBlend					=ONE;
	DestBlend					=ONE;
	BlendOp						=MIN;
	SrcBlendAlpha				=ONE;
	DestBlendAlpha				=ONE;
	BlendOpAlpha				=MIN;
	RenderTargetWriteMask[0]	=0x0F;
};

BlendState NoBlending
{
	AlphaToCoverageEnable	=FALSE;
	BlendEnable[0]			=FALSE;
	BlendEnable[1]			=FALSE;
	BlendEnable[2]			=FALSE;
	BlendEnable[3]			=FALSE;
	BlendEnable[4]			=FALSE;
	BlendEnable[5]			=FALSE;
	BlendEnable[6]			=FALSE;
	BlendEnable[7]			=FALSE;
};

BlendState ShadowBlending
{
	AlphaToCoverageEnable		=FALSE;
	BlendEnable[0]				=TRUE;
	BlendEnable[1]				=TRUE;
	BlendEnable[2]				=TRUE;
	BlendEnable[3]				=TRUE;
	BlendEnable[4]				=TRUE;
	BlendEnable[5]				=TRUE;
	BlendEnable[6]				=TRUE;
	BlendEnable[7]				=TRUE;
	SrcBlend					=ONE;
	DestBlend					=ONE;
	BlendOp						=REV_SUBTRACT;
	SrcBlendAlpha				=ONE;
	DestBlendAlpha				=ONE;
	BlendOpAlpha				=ADD;
	RenderTargetWriteMask[0]	=0x0F;
};

DepthStencilState EnableDepth
{
	DepthEnable		=TRUE;
	DepthWriteMask	=ALL;
	DepthFunc		=LESS;
};

DepthStencilState ShadowDepth
{
	DepthEnable		=TRUE;
	DepthWriteMask	=ZERO;
	DepthFunc		=EQUAL;
};

DepthStencilState DisableDepth
{
	DepthEnable		=FALSE;
	DepthWriteMask	=ZERO;
	DepthFunc		=ALWAYS;
};

DepthStencilState DisableDepthWrite
{
	DepthEnable		=TRUE;
	DepthWriteMask	=ZERO;
};

DepthStencilState DisableDepthTest
{
	DepthEnable		=TRUE;
	DepthWriteMask	=ALL;
	DepthFunc		=ALWAYS;
};

RasterizerState NoCull
{
	CullMode	=NONE;
};

SamplerState LinearClamp
{
	Filter		=MIN_MAG_MIP_LINEAR;
	AddressU	=Clamp;
	AddressV	=Clamp;
};

SamplerState LinearWrap
{
	Filter		=MIN_MAG_MIP_LINEAR;
	AddressU	=Wrap;
	AddressV	=Wrap;
};

SamplerState PointClamp
{
	Filter		=MIN_MAG_MIP_POINT;
	AddressU	=Clamp;
	AddressV	=Clamp;
};

SamplerState PointWrap
{
	Filter		=MIN_MAG_MIP_POINT;
	AddressU	=Wrap;
	AddressV	=Wrap;
};

SamplerState LinearClampCube
{
	Filter		=MIN_MAG_MIP_LINEAR;
	AddressU	=Clamp;
	AddressV	=Clamp;
	AddressW	=Clamp;
};

SamplerState LinearWrapCube
{
	Filter		=MIN_MAG_MIP_LINEAR;
	AddressU	=Wrap;
	AddressV	=Wrap;
	AddressW	=Wrap;
};

SamplerState PointClampCube
{
	Filter		=MIN_MAG_MIP_POINT;
	AddressU	=Clamp;
	AddressV	=Clamp;
	AddressW	=Clamp;
};

SamplerState PointWrapCube
{
	Filter		=MIN_MAG_MIP_POINT;
	AddressU	=Wrap;
	AddressV	=Wrap;
	AddressW	=Wrap;
};

SamplerState PointClamp1D
{
	Filter		=MIN_MAG_MIP_POINT;
	AddressU	=Clamp;
};

#endif	//_RENDERSTATESFXH