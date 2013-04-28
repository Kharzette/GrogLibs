using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
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
			public bool					mbSignedIn;
			public GamePadState			mGPS, mLastGPS;
			public KeyboardState		mKBS, mLastKBS;
			public MouseState			mMS, mLastMS;
			public SignedInGamer		mGamer;
			public AvatarDescription	mAvatarDesc;
			public AvatarRenderer		mAvatarRenderer;
			public PlayerIndex			mIndex;


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

			public bool AnyButtonHit(List<Buttons> buttons)
			{
				foreach(Buttons b in buttons)
				{
					if(WasButtonPressed(b))
					{
						return	true;
					}
				}
				return	false;
			}

			public void SetPresence(int pv)
			{
				if(mGamer != null)
				{
					if(mGamer.Presence != null)
					{
						mGamer.Presence.PresenceValue	=pv;
					}
				}
			}

			public void SetPresenceMode(GamerPresenceMode gpm)
			{
				if(mGamer != null)
				{
					if(mGamer.Presence != null)
					{
						mGamer.Presence.PresenceMode	=gpm;
					}
				}
			}

			//buttons have been responded to
			//so set the new / old equal so no
			//more of the same event goes off
			public void ClearInputs()
			{
				mLastGPS	=mGPS;
			}

			internal bool StartOrAHit(List<Buttons> mButtons)
			{
				if(WasButtonPressed(Buttons.Start))
				{
					return	true;
				}
				if(WasButtonPressed(Buttons.A))
				{
					return	true;
				}
				return	false;
			}
		}

		bool	mbGamerServicesAdded;

		PlayerInput	mPlayer1	=new PlayerInput();
		PlayerInput	mPlayer2	=new PlayerInput();
		PlayerInput	mPlayer3	=new PlayerInput();
		PlayerInput	mPlayer4	=new PlayerInput();

		//enum.getvalues not supported on xbox
		List<Buttons>	mButtons	=new List<Buttons>();


		public Input()
		{
			SignedInGamer.SignedIn	+=OnSignedIn;
			SignedInGamer.SignedOut	+=OnSignedOut;

			//no enum.getvalues on xbox
			mButtons.Add(Buttons.A);
			mButtons.Add(Buttons.B);
			mButtons.Add(Buttons.Back);
			mButtons.Add(Buttons.BigButton);
			mButtons.Add(Buttons.DPadDown);
			mButtons.Add(Buttons.DPadLeft);
			mButtons.Add(Buttons.DPadRight);
			mButtons.Add(Buttons.DPadUp);
			mButtons.Add(Buttons.LeftShoulder);
			mButtons.Add(Buttons.LeftStick);
			mButtons.Add(Buttons.LeftThumbstickDown);
			mButtons.Add(Buttons.LeftThumbstickLeft);
			mButtons.Add(Buttons.LeftThumbstickRight);
			mButtons.Add(Buttons.LeftThumbstickUp);
			mButtons.Add(Buttons.LeftTrigger);
			mButtons.Add(Buttons.RightShoulder);
			mButtons.Add(Buttons.RightStick);
			mButtons.Add(Buttons.RightThumbstickDown);
			mButtons.Add(Buttons.RightThumbstickLeft);
			mButtons.Add(Buttons.RightThumbstickRight);
			mButtons.Add(Buttons.RightThumbstickUp);
			mButtons.Add(Buttons.RightTrigger);
			mButtons.Add(Buttons.Start);
			mButtons.Add(Buttons.X);
			mButtons.Add(Buttons.Y);
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


		public void SetGamerServicesAdded(bool bAdded)
		{
			mbGamerServicesAdded	=bAdded;
		}


		public void ClearInputs()
		{
			if(mPlayer1 != null)
			{
				mPlayer1.ClearInputs();
			}
			if(mPlayer2 != null)
			{
				mPlayer2.ClearInputs();
			}
			if(mPlayer3 != null)
			{
				mPlayer3.ClearInputs();
			}
			if(mPlayer4 != null)
			{
				mPlayer4.ClearInputs();
			}
		}


		public bool StartOrAHit()
		{
			if(mPlayer1 != null)
			{
				if(mPlayer1.StartOrAHit(mButtons))
				{
					return	true;
				}
			}
			if(mPlayer2 != null)
			{
				if(mPlayer2.StartOrAHit(mButtons))
				{
					return	true;
				}
			}
			if(mPlayer3 != null)
			{
				if(mPlayer3.StartOrAHit(mButtons))
				{
					return	true;
				}
			}
			if(mPlayer4 != null)
			{
				if(mPlayer4.StartOrAHit(mButtons))
				{
					return	true;
				}
			}
			return	false;
		}


		public bool AnyButtonHit()
		{
			if(mPlayer1 != null)
			{
				if(mPlayer1.AnyButtonHit(mButtons))
				{
					return	true;
				}
			}
			if(mPlayer2 != null)
			{
				if(mPlayer2.AnyButtonHit(mButtons))
				{
					return	true;
				}
			}
			if(mPlayer3 != null)
			{
				if(mPlayer3.AnyButtonHit(mButtons))
				{
					return	true;
				}
			}
			if(mPlayer4 != null)
			{
				if(mPlayer4.AnyButtonHit(mButtons))
				{
					return	true;
				}
			}
			return	false;
		}


		public bool AnyKeyHit()
		{
			if(mPlayer1 == null)
			{
				return	false;
			}

			Keys	[]keys		=mPlayer1.mKBS.GetPressedKeys();
			Keys	[]lastKeys	=mPlayer1.mLastKBS.GetPressedKeys();

			if(keys.Length != lastKeys.Length)
			{
				return	true;
			}
			return	false;
		}


		public void RefreshAvatar(PlayerInput pi)
		{
			AvatarDescription.BeginGetFromGamer(pi.mGamer, LoadAvatar,
				new Nullable<PlayerIndex>(pi.mGamer.PlayerIndex));
		}


		public void Update()
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

			PlayerInput	pinp	=null;
			if(pi.Value == PlayerIndex.One)
			{
				pinp	=mPlayer1;
			}
			else if(pi.Value == PlayerIndex.Two)
			{
				pinp	=mPlayer2;
			}
			else if(pi.Value == PlayerIndex.Three)
			{
				pinp	=mPlayer3;
			}
			else if(pi.Value == PlayerIndex.Four)
			{
				pinp	=mPlayer4;
			}

			if(ad.IsValid)
			{
				//uncomment for an avatar description dump
				//for use with AvatarDescMaker tool
				//DumpAvatarToDebug(pinp, ad);
				ar	=new AvatarRenderer(ad);
			}
			else
			{
				ad	=AvatarDescription.CreateRandom();
				ar	=new AvatarRenderer(ad);
			}

			pinp.mAvatarDesc		=ad;
			pinp.mAvatarRenderer	=ar;
			
			if(pi.Value == PlayerIndex.One)
			{
				ad.Changed	+=OnPlayer1AvatarChanged;
			}
			else if(pi.Value == PlayerIndex.Two)
			{
				ad.Changed	+=OnPlayer2AvatarChanged;
			}
			else if(pi.Value == PlayerIndex.Three)
			{
				ad.Changed	+=OnPlayer3AvatarChanged;
			}
			else if(pi.Value == PlayerIndex.Four)
			{
				ad.Changed	+=OnPlayer4AvatarChanged;
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

			GC.Collect();
		}


		void OnSignedIn(object sender, EventArgs ea)
		{
			SignedInEventArgs	siea	=ea as SignedInEventArgs;

			Debug.WriteLine(siea.Gamer.DisplayName);
			Debug.WriteLine(siea.Gamer.Gamertag);
			Debug.WriteLine(siea.Gamer.IsDisposed);
			Debug.WriteLine(siea.Gamer.IsGuest);
			Debug.WriteLine(siea.Gamer.IsSignedInToLive);
			Debug.WriteLine(siea.Gamer.PlayerIndex);
			Debug.WriteLine(siea.Gamer.Tag);

			if(siea == null)
			{
				return;
			}

			if(siea.Gamer.PlayerIndex == PlayerIndex.One)
			{
				lock(mPlayer1)
				{
					mPlayer1.mbSignedIn	=true;
					mPlayer1.mGamer		=siea.Gamer;
					mPlayer1.mIndex		=PlayerIndex.One;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Two)
			{
				lock(mPlayer2)
				{
					mPlayer2.mbSignedIn	=true;
					mPlayer2.mGamer		=siea.Gamer;
					mPlayer2.mIndex		=PlayerIndex.Two;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Three)
			{
				lock(mPlayer3)
				{
					mPlayer3.mbSignedIn	=true;
					mPlayer3.mGamer		=siea.Gamer;
					mPlayer3.mIndex		=PlayerIndex.Three;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Four)
			{
				lock(mPlayer4)
				{
					mPlayer4.mbSignedIn	=true;
					mPlayer4.mGamer		=siea.Gamer;
					mPlayer4.mIndex		=PlayerIndex.Four;
				}
				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
		}


		void DumpAvatarToDebug(PlayerInput pi, AvatarDescription ad)
		{
			Debug.WriteLine("Avatar description for " + pi.mGamer.Gamertag);
			for(int i=0;i < ad.Description.Length;i++)
			{
				Debug.WriteLine("" + ad.Description[i]);
			}
		}


		void UpdatePlayer(PlayerInput pi, GamePadState gps)
		{
			lock(pi)
			{
				if(!pi.mbActive && mbGamerServicesAdded)
				{
					pi.mbActive	=true;

					//if the player is not signed in, they might
					//not have an avatar description
					if(!pi.mbSignedIn)
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
			pi.mbSignedIn		=false;
			pi.mGamer			=null;
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
