using System;
using System.Collections.Generic;
using UtilityLib;
using BSPZone;
using SharpDX;


namespace EntityLib
{
	public class TriggerAble : Component
	{
		public enum TState
		{
			Triggered
		}

		bool	mbTriggered;


		public TriggerAble(Entity owner) : base(owner)
		{
		}


		public override void Update(UpdateTimer time)
		{
		}


		public override void StateChange(Enum state, UInt32 value)
		{
			if(!state.Equals(TState.Triggered))
			{
				return;
			}

			mbTriggered	=(value != 0);

			//notify owner components
			List<Component>	comps	=mOwner.GetComponents();
			foreach(Component c in comps)
			{
				if(c is BModelMover)
				{
					c.StateChange(BModelMover.States.Forward, value);
					c.StateChange(BModelMover.States.Moving, value);
				}
				else if(c is Trigger)
				{
					Trigger	trig	=c as Trigger;
					trig.TriggerTarget(value);
				}
				else if(c is Light)
				{
					Light	lt	=c as Light;

					UInt32	lightToggle	=lt.IsOn()? 0u : 1u;
					c.StateChange(Light.State.On, lightToggle);
				}
			}
		}
	}
}