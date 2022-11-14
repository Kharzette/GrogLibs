//shader for doing an old style "character mode" like old
//pc CGA 40x25 text mode etc
#include	"Types.hlsli"
#include	"CommonFunctions.hlsli"

//contents of the screen
Texture1D<uint>	mScreenContents : register(t0);

//font texture
Texture2D		mFont : register(t1);

cbuffer	TextMode : register(b7)
{
	uint	mWidth, mHeight;	//dimensions of screen in pixels
	uint	mCWidth, mCHeight;	//dimensions of screen in character blocks

	//font texture info
	uint	mStartChar;		//first letter of the font bitmap
	uint	mNumColumns;	//number of font columns in the font texture
	uint	mCharWidth;		//width of characters in texels in the font texture (fixed)
	uint	mCharHeight;	//height of characters in texels in the font texture (fixed)
}


VVPos	SimpleVS(float3 pos : POSITION)
{
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);

	VVPos	ret;

	ret.Position	=mul(float4(pos, 1), wvp);

	return	ret;
}

float4	TextModePS(float4 pos : SV_POSITION) : SV_Target
{
	//scales from screen pixels to "blocks" for the fixed width columns and rows
	float2	screenToBlockScale	=float2(mCWidth / (float)mWidth,
									mCHeight / (float)mHeight);

	//get xy in screen scale
	int2	xyText	=int2(pos.x * screenToBlockScale.x,
						  pos.y * screenToBlockScale.y);
	float2	xyPix	=float2(pos.x * screenToBlockScale.x,
							pos.y * screenToBlockScale.y);

	//distance from topleft of character in blockspace
	xyPix	-=xyText;

	//convert character coordinate to 1D
	int	coord	=(xyText.y * mCWidth) + xyText.x;

	int2	loadCoord	=int2(coord, 0);

	//look up character
	uint	char	=mScreenContents.Load(loadCoord);

	int	posOffset	=char - mStartChar;
	if(posOffset < 0)
	{
		return	float4(0, 0, 0, 0);
	}

	//get xy of character within the font texture
	uint	yOffset	=posOffset / mNumColumns;
	posOffset	%=mNumColumns;

	//scale up to pixels in the font texture
	yOffset		*=mCharHeight;
	posOffset	*=mCharWidth;
	xyPix.x		*=mCharWidth;
	xyPix.y		*=mCharHeight;

	//add in block space distance
	int3	fontPix	=int3(posOffset + xyPix.x,
						  yOffset + xyPix.y, 0);

	return	mFont.Load(fontPix);
}