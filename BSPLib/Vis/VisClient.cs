using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;


namespace BSPLib
{
	public class BuildFarmCaps
	{
		public int	mNumCores;	//number of cpu cores
		public uint	mMHZ;		//cpu speed
	}


	[ServiceContractAttribute(Namespace="http://Microsoft.ServiceModel.Samples", ConfigurationName="IMapVis")]
	public interface IMapVis
	{
		[OperationContractAttribute(Action="http://Microsoft.ServiceModel.Samples/IMapVis/FloodPortalsSlow",
			ReplyAction="http://Microsoft.ServiceModel.Samples/IMapVis/FloodPortalsSlowResponse")]
		byte	[]FloodPortalsSlow(byte []visData, int startPort, int endPort);

		[OperationContractAttribute(Action="http://Microsoft.ServiceModel.Samples/IMapVis/QueryCapabilities",
			ReplyAction="http://Microsoft.ServiceModel.Samples/IMapVis/QueryCapabilitiesResponse")]
		BuildFarmCaps QueryCapabilities();
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
		
		public byte []FloodPortalsSlow(byte []visData, int startPort, int endPort)
		{
			return base.Channel.FloodPortalsSlow(visData, startPort, endPort);
		}

		public BuildFarmCaps QueryCapabilities()
		{
			return	base.Channel.QueryCapabilities();
		}
	}
}
