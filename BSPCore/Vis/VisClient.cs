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
		BuildFarmCaps	QueryCapabilities();
	}
	
	
	public interface IMapVisChannel : IMapVis, IClientChannel
	{
	}


	[System.Diagnostics.DebuggerStepThroughAttribute()]
	public partial class MapVisClient : ClientBase<IMapVis>, IMapVis
	{
		public BuildFarmCaps	mBuildCaps;
		public bool				mbActive;
		public int				mNumFailures;


		public MapVisClient()
		{
		}
		
		public MapVisClient(string endpointConfigurationName) : base(endpointConfigurationName)
		{
		}
		
		public MapVisClient(string endpointConfigurationName, string remoteAddress)
			: base(endpointConfigurationName, remoteAddress)
		{
		}
		
		public MapVisClient(string endpointConfigurationName, EndpointAddress remoteAddress)
			: base(endpointConfigurationName, remoteAddress)
		{
		}
		
		public MapVisClient(Binding binding, EndpointAddress remoteAddress)
			: base(binding, remoteAddress)
		{
		}

		public byte []EndFloodPortalsSlow(IAsyncResult result)
		{
			return	base.Channel.EndFloodPortalsSlow(result);
		}
		
		public IAsyncResult BeginFloodPortalsSlow(object visState, AsyncCallback callBack, object aSyncState)
		{
			return	base.Channel.BeginFloodPortalsSlow(visState, callBack, aSyncState);
		}

		public BuildFarmCaps QueryCapabilities()
		{
			return	base.Channel.QueryCapabilities();
		}
	}
}
