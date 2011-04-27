using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Configuration;
using System.Runtime.Remoting.Messaging;
using BSPLib;


namespace BuildWCF
{
	[ServiceContract(Namespace="http://Microsoft.ServiceModel.Samples")]
	public interface IMapVis
	{
		[OperationContract]
		byte	[]FloodPortalsSlow(byte []visData, int startPort, int endPort);

		[OperationContract]
		BuildFarmCaps	QueryCapabilities();
	}


	public class MapVisService : IMapVis
	{
		public byte []FloodPortalsSlow(byte []visData, int startPort, int endPort)
		{
			return	Map.FloodPortalsSlow(visData, startPort, endPort);
		}

		public BuildFarmCaps QueryCapabilities()
		{
			BuildFarmCaps	ret	=new BuildFarmCaps();
			
			int	coreCount	=0;
			foreach(var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
			{
				coreCount	+=int.Parse(item["NumberOfCores"].ToString());
			}
			ret.mNumCores	=coreCount;

			System.Management.ManagementObject	Mo	=
				new System.Management.ManagementObject("Win32_Processor.DeviceID='CPU0'");
			
			ret.mMHZ	=(uint)(Mo["CurrentClockSpeed"]);
			Mo.Dispose();
			
			return	ret;
		}
	}


	public class HaxdServiceHost : ServiceHost
	{
		public HaxdServiceHost(Type type, Uri baseAddr) : base(type, baseAddr)
		{
		}

		protected override void ApplyConfiguration()
		{
			// workaround for passing a custom configFilename
			string configFilename = (string)CallContext.GetData("_config");

			configFilename	=AppDomain.CurrentDomain.BaseDirectory + "App.config";

			Console.WriteLine(configFilename);
			
			ExeConfigurationFileMap filemap = new ExeConfigurationFileMap();
			
			filemap.ExeConfigFilename =
				string.IsNullOrEmpty(configFilename) ?
				AppDomain.CurrentDomain.SetupInformation.ConfigurationFile : configFilename;
			
			Configuration config =
				ConfigurationManager.OpenMappedExeConfiguration(filemap, ConfigurationUserLevel.None);
			
			ServiceModelSectionGroup serviceModel = ServiceModelSectionGroup.GetSectionGroup(config);
			foreach (ServiceElement se in serviceModel.Services.Services)
			{
				if (se.Name == this.Description.ConfigurationName)
				{
					base.LoadConfigurationSection(se);
					return;
				}
			}
			throw new ArgumentException("ServiceElement doesn't exist");
		}
	}


	class Program
	{
		static void Main(string[] args)
		{
			//Step 1 of the address configuration procedure: Create a URI to serve as the base address.
			Uri	baseAddr	=new Uri("http://localhost:8000/ServiceModelSamples/Service");
			
			//Step 2 of the hosting procedure: Create ServiceHost
//			HaxdServiceHost selfHost = new HaxdServiceHost(typeof(MapVisService), baseAddr);
			ServiceHost selfHost = new ServiceHost(typeof(MapVisService), baseAddr);

			try
			{
				// Step 3 of the hosting procedure: Add a service endpoint.
				selfHost.AddServiceEndpoint(
					typeof(IMapVis),
					new WSHttpBinding("WSHttpBinding_MapVisService"),
					"MapVisService");
				
				//Step 4 of the hosting procedure: Enable metadata exchange.
				ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
				
				smb.HttpGetEnabled = true;
				
				selfHost.Description.Behaviors.Add(smb);
				
				// Step 5 of the hosting procedure: Start (and then stop) the service.
				selfHost.Open();
				Console.WriteLine("The service is ready.");
				Console.WriteLine("Press <ENTER> to terminate service.");
				Console.WriteLine();
				Console.ReadLine();
				
				// Close the ServiceHostBase to shutdown the service.
				selfHost.Close();
			}
			catch (CommunicationException ce)
			{
				Console.WriteLine("An exception occurred: {0}", ce.Message);
				selfHost.Abort();
			}
		}
	}
}
