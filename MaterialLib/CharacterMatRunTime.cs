using Vortice.Direct3D11;


namespace MaterialLib;

//Material stuff specific to characters
internal partial class CharacterMat
{
	//bones are directly set in meshlib
	//so nothing here right now
	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
	}
}