using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;


namespace UtilityLib
{
	public class Input
	{
		public class PlayerInput
		{
			public bool					mbActive;
			public bool					mbSigningIn;
			public GamePadState			mGPS, mLastGPS;
			public KeyboardState		mKBS, mLastKBS;
			public MouseState			mMS, mLastMS;
			public SignedInGamer		mGamer;
			public AvatarDescription	mAvatarDesc;
			public AvatarRenderer		mAvatarRenderer;


			//looks for a press and release
			public bool WasKeyPressed(Keys key)
			{
				if(mKBS.IsKeyUp(key))
				{
					if(mLastKBS.IsKeyDown(key))
					{
						return	true;	//press and release
					}
				}
				return	false;
			}

			//look for a press and release
			public bool WasButtonPressed(Buttons but)
			{
				if(mGPS.IsButtonUp(but))
				{
					if(mLastGPS.IsButtonDown(but))
					{
						return	true;
					}
				}
				return	false;
			}
		}

		PlayerInput	mPlayer1	=new PlayerInput();
		PlayerInput	mPlayer2	=new PlayerInput();
		PlayerInput	mPlayer3	=new PlayerInput();
		PlayerInput	mPlayer4	=new PlayerInput();


		public Input()
		{
			SignedInGamer.SignedIn	+=OnSignedIn;
			SignedInGamer.SignedOut	+=OnSignedOut;
		}


		public PlayerInput Player1
		{
			get
			{
#if XBOX
				if(mPlayer1.mbActive)
				{
					return	mPlayer1;
				}
				return	null;
#else
				return	mPlayer1;
#endif
			}
		}

		public PlayerInput Player2
		{
			get
			{
				if(mPlayer2.mbActive)
				{
					return	mPlayer2;
				}
				return	null;
			}
		}

		public PlayerInput Player3
		{
			get
			{
				if(mPlayer3.mbActive)
				{
					return	mPlayer3;
				}
				return	null;
			}
		}

		public PlayerInput Player4
		{
			get
			{
				if(mPlayer4.mbActive)
				{
					return	mPlayer4;
				}
				return	null;
			}
		}


		public void Update(float msDelta)
		{
			GamePadState	gps	=GamePad.GetState(PlayerIndex.One);
			if(gps.IsConnected)
			{
				UpdatePlayer(mPlayer1, gps);
			}
			else
			{
				mPlayer1.mLastGPS	=mPlayer1.mGPS;
				mPlayer1.mGPS		=gps;
			}

			gps	=GamePad.GetState(PlayerIndex.Two);
			if(gps.IsConnected)
			{
				UpdatePlayer(mPlayer2, gps);
			}
			else
			{
				mPlayer2.mLastGPS	=mPlayer2.mGPS;
				mPlayer2.mGPS		=gps;
			}

			gps	=GamePad.GetState(PlayerIndex.Three);
			if(gps.IsConnected)
			{
				UpdatePlayer(mPlayer3, gps);
			}
			else
			{
				mPlayer3.mLastGPS	=mPlayer3.mGPS;
				mPlayer3.mGPS		=gps;
			}

			gps	=GamePad.GetState(PlayerIndex.Four);
			if(gps.IsConnected)
			{
				UpdatePlayer(mPlayer4, gps);
			}
			else
			{
				mPlayer4.mLastGPS	=mPlayer4.mGPS;
				mPlayer4.mGPS		=gps;
			}

			//keyboard and mouse go to player 1
			mPlayer1.mLastKBS	=mPlayer1.mKBS;
			mPlayer1.mLastMS	=mPlayer1.mMS;

			mPlayer1.mKBS	=Keyboard.GetState();
			mPlayer1.mMS	=Mouse.GetState();
		}


		void LoadAvatar(IAsyncResult res)
		{
			Nullable<PlayerIndex>	pi	=res.AsyncState as Nullable<PlayerIndex>;

			if(pi == null)
			{
				return;
			}

			AvatarDescription	ad	=AvatarDescription.EndGetFromGamer(res);
			AvatarRenderer		ar	=null;

			if(ad == null)
			{
				return;
			}

			if(ad.IsValid)
			{
				ar	=new AvatarRenderer(ad);
			}
			else
			{
				ad	=AvatarDescription.CreateRandom();
				ar	=new AvatarRenderer(ad);
			}

			if(pi.Value == PlayerIndex.One)
			{
				mPlayer1.mAvatarDesc		=ad;
				mPlayer1.mAvatarRenderer	=ar;
				ad.Changed					+=OnPlayer1AvatarChanged;
			}
			else if(pi.Value == PlayerIndex.Two)
			{
				mPlayer2.mAvatarDesc		=ad;
				mPlayer2.mAvatarRenderer	=ar;
				ad.Changed					+=OnPlayer2AvatarChanged;
			}
			else if(pi.Value == PlayerIndex.Three)
			{
				mPlayer3.mAvatarDesc		=ad;
				mPlayer3.mAvatarRenderer	=ar;
				ad.Changed					+=OnPlayer3AvatarChanged;
			}
			else if(pi.Value == PlayerIndex.Four)
			{
				mPlayer4.mAvatarDesc		=ad;
				mPlayer4.mAvatarRenderer	=ar;
				ad.Changed					+=OnPlayer4AvatarChanged;
			}
		}


		void OnSignedOut(object sender, EventArgs ea)
		{
			SignedOutEventArgs	siea	=ea as SignedOutEventArgs;

			if(siea == null)
			{
				return;
			}

			if(siea.Gamer.PlayerIndex == PlayerIndex.One)
			{
				FreePlayerInput(mPlayer1);
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Two)
			{
				FreePlayerInput(mPlayer2);
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Three)
			{
				FreePlayerInput(mPlayer3);
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Four)
			{
				FreePlayerInput(mPlayer4);
			}
		}


		void OnSignedIn(object sender, EventArgs ea)
		{
			SignedInEventArgs	siea	=ea as SignedInEventArgs;

			if(siea == null)
			{
				return;
			}

			if(siea.Gamer.PlayerIndex == PlayerIndex.One)
			{
				lock(mPlayer1)
				{
					mPlayer1.mbSigningIn	=true;
					mPlayer1.mGamer			=siea.Gamer;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Two)
			{
				lock(mPlayer2)
				{
					mPlayer2.mbSigningIn	=true;
					mPlayer2.mGamer			=siea.Gamer;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Three)
			{
				lock(mPlayer3)
				{
					mPlayer3.mbSigningIn	=true;
					mPlayer3.mGamer			=siea.Gamer;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Four)
			{
				lock(mPlayer4)
				{
					mPlayer4.mbSigningIn	=true;
					mPlayer4.mGamer			=siea.Gamer;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
		}


		void UpdatePlayer(PlayerInput pi, GamePadState gps)
		{
			lock(pi)
			{
				if(!pi.mbActive)
				{
					pi.mbActive	=true;

					//if the player is not signed in, they might
					//not have an avatar description
					if(!pi.mbSigningIn)
					{
						pi.mAvatarDesc		=AvatarDescription.CreateRandom();
						pi.mAvatarRenderer	=new AvatarRenderer(pi.mAvatarDesc);
					}
				}
				pi.mLastGPS	=pi.mGPS;
				pi.mGPS		=gps;
			}
		}


		void FreePlayerInput(PlayerInput pi)
		{
			pi.mbActive	=false;

			if(pi.mGamer.PlayerIndex == PlayerIndex.One)
			{
				pi.mAvatarDesc.Changed	-=OnPlayer1AvatarChanged;
			}
			else if(pi.mGamer.PlayerIndex == PlayerIndex.Two)
			{
				pi.mAvatarDesc.Changed	-=OnPlayer2AvatarChanged;
			}
			else if(pi.mGamer.PlayerIndex == PlayerIndex.Three)
			{
				pi.mAvatarDesc.Changed	-=OnPlayer3AvatarChanged;
			}
			else if(pi.mGamer.PlayerIndex == PlayerIndex.Four)
			{
				pi.mAvatarDesc.Changed	-=OnPlayer4AvatarChanged;
			}

			pi.mAvatarDesc		=null;
			pi.mAvatarRenderer	=null;
			pi.mbSigningIn		=false;
		}


		void OnPlayer1AvatarChanged(object sender, EventArgs ea)
		{
			AvatarDescription.BeginGetFromGamer(mPlayer1.mGamer, LoadAvatar,
				new Nullable<PlayerIndex>(PlayerIndex.One));
		}


		void OnPlayer2AvatarChanged(object sender, EventArgs ea)
		{
			AvatarDescription.BeginGetFromGamer(mPlayer2.mGamer, LoadAvatar,
				new Nullable<PlayerIndex>(PlayerIndex.Two));
		}


		void OnPlayer3AvatarChanged(object sender, EventArgs ea)
		{
			AvatarDescription.BeginGetFromGamer(mPlayer3.mGamer, LoadAvatar,
				new Nullable<PlayerIndex>(PlayerIndex.Three));
		}


		void OnPlayer4AvatarChanged(object sender, EventArgs ea)
		{
			AvatarDescription.BeginGetFromGamer(mPlayer4.mGamer, LoadAvatar,
				new Nullable<PlayerIndex>(PlayerIndex.Four));
		}
	}
}
