using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;


namespace BSPCore
{
	public class BuildFarmCaps
	{
		public int	mNumCores;	//number of cpu cores
		public uint	mMHZ;		//cpu speed
	}


	[ServiceContract(Namespace="http://Microsoft.ServiceModel.Samples")]
	[ServiceKnownType(typeof(VisState))]
	public interface IMapVis
	{
		[OperationContract(AsyncPattern = true)]
		IAsyncResult	BeginFloodPortalsSlow(object visState, AsyncCallback callBack, object aSyncState);

		//no operationcontract for the end method
		byte	[]EndFloodPortalsSlow(IAsyncResult result);

		[OperationContract]
		bool	HasPortals(object visState);

		[OperationContract]
		bool	ReadPortals(object visState);

		[OperationContract]
		bool	FreePortals();

		[OperationContract]
		VisState	GetAbandoned(int startPort, int endPort);

		[OperationContract]
		BuildFarmCaps	QueryCapabilities();
	}
	
	
	public interface IMapVisChannel : IMapVis, IClientChannel
	{
	}


//	[System.Diagnostics.DebuggerStepThroughAttribute()]
	public partial class MapVisClient : ClientBase<IMapVis>, IMapVis
	{
		public BuildFarmCaps	mBuildCaps;
		public int				mNumFailures;
		public string			mEndPointURI;


		public MapVisClient()
		{
		}
		
		public MapVisClient(string endpointConfigurationName) : base(endpointConfigurationName)
		{
		}
		
		public MapVisClient(string endpointConfigurationName, string remoteAddress)
			: base(endpointConfigurationName, remoteAddress)
		{
			mEndPointURI	=remoteAddress;
		}
		
		public MapVisClient(string endpointConfigurationName, EndpointAddress remoteAddress)
			: base(endpointConfigurationName, remoteAddress)
		{
			mEndPointURI	=remoteAddress.ToString();
		}
		
		public MapVisClient(Binding binding, EndpointAddress remoteAddress)
			: base(binding, remoteAddress)
		{
			mEndPointURI	=remoteAddress.ToString();
		}

		public byte []EndFloodPortalsSlow(IAsyncResult result)
		{
			byte	[]ret	=null;

			//this can fire a comm exception if
			//the client service is killed off mid process
			try
			{
				ret	=base.Channel.EndFloodPortalsSlow(result);
			}
			catch
			{
			}
			return	ret;
		}
		
		public IAsyncResult BeginFloodPortalsSlow(object visState, AsyncCallback callBack, object aSyncState)
		{
			return	base.Channel.BeginFloodPortalsSlow(visState, callBack, aSyncState);
		}

		public bool HasPortals(object visState)
		{
			return	base.Channel.HasPortals(visState);
		}

		public bool ReadPortals(object visState)
		{
			return	base.Channel.ReadPortals(visState);
		}

		public bool FreePortals()
		{
			return	base.Channel.FreePortals();
		}

		public VisState GetAbandoned(int startPort, int endPort)
		{
			return	base.Channel.GetAbandoned(startPort, endPort);
		}

		public BuildFarmCaps QueryCapabilities()
		{
			return	base.Channel.QueryCapabilities();
		}

		public bool IsReadyOrTrashed(out bool bRecreate)
		{
			bRecreate	=false;

			if(State == System.ServiceModel.CommunicationState.Opened)
			{
				return	true;
			}

			if(State == System.ServiceModel.CommunicationState.Faulted)
			{
				bRecreate	=true;
				Abort();
				return	false;
			}
			else if(State == System.ServiceModel.CommunicationState.Closed)
			{
				bRecreate	=true;
				return	false;
			}
			else if(State == System.ServiceModel.CommunicationState.Created)
			{
				try
				{
					Open();
				}
				catch
				{
				}
				return	false;
			}

			//transitioning
			return	false;
		}
	}
}
