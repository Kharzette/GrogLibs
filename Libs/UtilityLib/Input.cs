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
			public GamePadState			mGPS, mLastGPS;
			public KeyboardState		mKBS, mLastKBS;
			public MouseState			mMS, mLastMS;
			public SignedInGamer		mGamer;
			public AvatarDescription	mAvatarDesc;
			public AvatarRenderer		mAvatarRenderer;
		}

		PlayerInput	mPlayer1	=new PlayerInput();
		PlayerInput	mPlayer2	=new PlayerInput();
		PlayerInput	mPlayer3	=new PlayerInput();
		PlayerInput	mPlayer4	=new PlayerInput();

		List<PlayerInput>	mActives	=new List<PlayerInput>();


		public Input()
		{
			SignedInGamer.SignedIn	+=OnSignedIn;
		}


		public PlayerInput Player1
		{
			get
			{
				if(mActives.Count > 0)
				{
					return	mActives[0];
				}
				return	null;
			}
		}

		public PlayerInput Player2
		{
			get
			{
				if(mActives.Count > 1)
				{
					return	mActives[1];
				}
				return	null;
			}
		}

		public PlayerInput Player3
		{
			get
			{
				if(mActives.Count > 2)
				{
					return	mActives[2];
				}
				return	null;
			}
		}

		public PlayerInput Player4
		{
			get
			{
				if(mActives.Count > 3)
				{
					return	mActives[3];
				}
				return	null;
			}
		}


		public void Update(float msDelta)
		{
			mActives.Clear();

			GamePadState	gps	=GamePad.GetState(PlayerIndex.One);
			if(gps.IsConnected)
			{
				mPlayer1.mbActive	=true;
				mPlayer1.mLastGPS	=mPlayer1.mGPS;
				mPlayer1.mGPS		=gps;
				mActives.Add(mPlayer1);
			}

			gps	=GamePad.GetState(PlayerIndex.Two);
			if(gps.IsConnected)
			{
				mPlayer2.mbActive	=true;
				mPlayer2.mLastGPS	=mPlayer1.mGPS;
				mPlayer2.mGPS		=gps;
				mActives.Add(mPlayer2);
			}

			gps	=GamePad.GetState(PlayerIndex.Three);
			if(gps.IsConnected)
			{
				mPlayer3.mbActive	=true;
				mPlayer3.mLastGPS	=mPlayer1.mGPS;
				mPlayer3.mGPS		=gps;
				mActives.Add(mPlayer3);
			}

			gps	=GamePad.GetState(PlayerIndex.Four);
			if(gps.IsConnected)
			{
				mPlayer4.mbActive	=true;
				mPlayer4.mLastGPS	=mPlayer1.mGPS;
				mPlayer4.mGPS		=gps;
				mActives.Add(mPlayer4);
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


		void OnSignedIn(object sender, EventArgs ea)
		{
			SignedInEventArgs	siea	=ea as SignedInEventArgs;

			if(siea == null)
			{
				return;
			}

			if(siea.Gamer.PlayerIndex == PlayerIndex.One)
			{
				mPlayer1.mGamer	=siea.Gamer;

				AvatarDescription.BeginGetFromGamer(siea.Gamer, LoadAvatar,
					new Nullable<PlayerIndex>(siea.Gamer.PlayerIndex));
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Two)
			{
				mPlayer2.mGamer	=siea.Gamer;
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Three)
			{
				mPlayer3.mGamer	=siea.Gamer;
			}
			else if(siea.Gamer.PlayerIndex == PlayerIndex.Four)
			{
				mPlayer4.mGamer	=siea.Gamer;
			}

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
