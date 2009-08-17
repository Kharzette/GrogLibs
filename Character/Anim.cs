using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class GameAnim
	{
		List<GameSubAnim>	[]mControllerAnims;
		Matrix				[]mInverseBindPoses;


		public GameAnim(int numControllers, List<Matrix> invBinds)
		{
			mControllerAnims	=new List<GameSubAnim>[numControllers];

			mInverseBindPoses	=new Matrix[invBinds.Count];
			for(int i=0;i < invBinds.Count;i++)
			{
				mInverseBindPoses[i]	=invBinds[i];
			}
		}


		public void AddControllerSubAnims(int cidx, List<GameSubAnim> anims)
		{
			mControllerAnims[cidx]	=anims;
		}


		public void Animate(int cidx, float time, GameSkeleton gs)
		{
			List<GameSubAnim>	subs	=mControllerAnims[cidx];

			foreach(GameSubAnim an in subs)
			{
				an.Animate(time, gs);
			}
		}


		public void ApplyBindShapes(int cidx, Matrix []bones)
		{
			for(int i=0;i < bones.Length;i++)
			{
				bones[i]	=mInverseBindPoses[cidx] * bones[i];
			}
		}
	}
}