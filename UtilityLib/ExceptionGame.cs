﻿using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;


namespace UtilityLib
{
	public class ExceptionGame : Game
	{
		const string	ErrorTitle		="Unexpected Error";
		const string	ErrorMessage	=
			"The game encountered an unexpected error and shut itself down.  " +
			"Apologies for any inconvenience.";

		static readonly string[]	ErrorButtons	=new[]
		{
			"Exit to Dashboard",
			"View Error Details"
		};

		readonly	Exception	mException;

		object	mCaller;
		bool	mbShownMessage;
		bool	mbDisplayException;

		SpriteBatch	mSB;
		SpriteFont	mFont;


		public ExceptionGame(Exception e)
		{
			new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth	=1280,
				PreferredBackBufferHeight	=720
			};

			mException	=e;

			Content.RootDirectory	="GameContent";
		}


		public void SetCaller(object obj)
		{
			mCaller	=obj;
		}


		protected override void Initialize()
		{
			GamerServicesComponent	gsc	=null;

			PropertyInfo	prop	=mCaller.GetType().GetProperty("GamerServices");
			if(prop != null)
			{
				object	val	=prop.GetValue(mCaller, null);
				if(val != null)
				{
					gsc	=val as GamerServicesComponent;
				}
			}

			bool	bNewGSC	=false;
			if(gsc == null)
			{
				gsc	=new GamerServicesComponent(this);
				Components.Add(gsc);
				bNewGSC	=true;
			}

			base.Initialize();

			if(!bNewGSC)
			{
				Components.Add(gsc);
			}
		}


		protected override void LoadContent()
		{
			mSB		=new SpriteBatch(GraphicsDevice);
			mFont	=Content.Load<SpriteFont>("Fonts/Pescadero12");

			base.LoadContent();
		}


		protected override void Update(GameTime gameTime)
		{
			if(!mbShownMessage)
			{
				try
				{
					if(!Guide.IsVisible)
					{
						Guide.BeginShowMessageBox(ErrorTitle,
							ErrorMessage, ErrorButtons, 0, MessageBoxIcon.Error,
							result =>
							{
								int?	choice	=Guide.EndShowMessageBox(result);

								if(choice.HasValue && choice.Value == 1)
								{
									mbDisplayException	=true;
								}
								else
								{
									Exit();
								}
							}, null);
						mbShownMessage	=true;
					}
				}
				catch
				{
				}
			}
			else
			{
				GamePadState	p1	=GamePad.GetState(PlayerIndex.One);
				GamePadState	p2	=GamePad.GetState(PlayerIndex.Two);
				GamePadState	p3	=GamePad.GetState(PlayerIndex.Three);
				GamePadState	p4	=GamePad.GetState(PlayerIndex.Four);

				if(p1.IsButtonDown(Buttons.Back)
					|| p2.IsButtonDown(Buttons.Back)
					|| p3.IsButtonDown(Buttons.Back)
					|| p4.IsButtonDown(Buttons.Back))
				{
					Exit();
				}
			}

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.Black);

			if(mbDisplayException)
			{
				mSB.Begin();
				mSB.DrawString(mFont, mException.ToString(),
					Vector2.UnitX * GraphicsDevice.Viewport.TitleSafeArea.X +
					Vector2.UnitY * GraphicsDevice.Viewport.TitleSafeArea.Y,
					Color.White);
				mSB.DrawString(mFont, "Press Back to Exit",
					Vector2.UnitX * GraphicsDevice.Viewport.TitleSafeArea.X +
					Vector2.UnitY * GraphicsDevice.Viewport.TitleSafeArea.Height,
					Color.White);
				mSB.End();
			}

			base.Draw(gameTime);
		}


		//from blog.nickgravelyn.com
		public static void Run<T>() where T : Game, new()
		{
			if(Debugger.IsAttached)
			{
				using(var g	=new T())
				{
					g.Run();
				}
			}
			else
			{
				Type	gameClassType	=typeof(object);
				object	gameObject		=null;
				try
				{
					using(var g	=new T())
					{
						gameObject		=g;
						gameClassType	=g.GetType();
						g.Run();
					}
				}
				catch(Exception e)
				{
					using(var g	=new ExceptionGame(e))
					{
						g.SetCaller(gameObject);
						g.Run();
					}
				}
			}
		}
	}
}