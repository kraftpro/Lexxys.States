using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Xml;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.States.Tests
{

	[TestClass]
	public class SampteTests
	{
		private static StatechartConfig ChartConfig;

		static SampteTests()
		{
			StaticServices.AddLoggingStab();
			StaticServices.ConfigService().AddConfiguration(new Uri(".\\sample-1.config.txt", UriKind.RelativeOrAbsolute));
			var root = Config.Current.GetValue<XmlLiteNode>("statecharts").Value;
			Assert.IsNotNull(root);
			var element = root.Element(o => o["name"] == "sample-1");
			Assert.IsFalse(element.IsEmpty);
			var config = StatechartConfig.FromXml(element);
			Assert.IsNotNull(config);
			ChartConfig = config;
		}

		Statechart<SampleObj> GetSample() => ChartConfig.Create<SampleObj>(actionBuilder: o => new EchoAction<SampleObj>(o));

		[TestMethod]
		public void A_CanCreateTest()
		{
			var sample = GetSample();
			Assert.IsNotNull(sample);
			Assert.IsFalse(sample.IsStarted);
			Assert.IsFalse(sample.IsFinished);
		}

		[TestMethod]
		public void B_CanStartTest()
		{
			var sample = GetSample();
			var obj = new SampleObj();
			sample.Start(obj);
			Assert.IsTrue(sample.IsStarted);
			Assert.IsFalse(sample.IsFinished);
		}

		[TestMethod]
		public void C_CanMoveAndFinalizeTest()
		{
			var sample = GetSample();
			var obj = new SampleObj();
			sample.Start(obj);
			Assert.IsTrue(sample.IsStarted);
			var abort = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Abort");
			Assert.IsNotNull(abort);
			sample.OnEvent(abort, obj);
			Assert.AreEqual("Aborted", sample.CurrentState.Name);
			Assert.IsTrue(sample.IsFinished);
		}

		[TestMethod]
		public void D_CanMoveToCompositStateTest()
		{
			Run_CanMoveToCompositState();
		}

		private (SampleObj, Statechart<SampleObj>) Run_CanMoveToCompositState()
		{
			var sample = GetSample();
			var obj = new SampleObj();
			sample.Start(obj);
			Assert.IsTrue(sample.IsStarted);
			var begin = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Begin");
			Assert.IsNotNull(begin);
			sample.OnEvent(begin, obj);
			Assert.AreEqual("Uploading", sample.CurrentState.Name);
			var docs = sample.CurrentState.Charts;
			Assert.IsNotNull(docs);
			Assert.AreEqual(2, docs.Count);
			var dd = docs.ToArray();
			Assert.IsTrue(dd[0].IsStarted);
			Assert.IsTrue(dd[1].IsStarted);

			var events = sample.GetActiveEvents(obj).ToList();
			Assert.IsNotNull(events);
			Assert.AreEqual(5, events.Count);
			return (obj, sample);
		}

		[TestMethod]
		public void E_CanHoldRestartTest()
		{
			var (obj, sample) = Run_CanHold();
			var restart = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Restart");
			Assert.IsNotNull(restart);
			sample.OnEvent(restart, obj);
			Assert.AreEqual("Ready", sample.CurrentState.Name);
			var begin = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Begin");
			Assert.IsNotNull(begin);
			sample.OnEvent(begin, obj);
			Assert.AreEqual("Uploading", sample.CurrentState.Name);

			var docs = sample.CurrentState.Charts;
			Assert.IsNotNull(docs);
			Assert.AreEqual(2, docs.Count);
			var dd = docs.ToArray();
			Assert.IsTrue(dd[0].IsStarted);
			Assert.IsTrue(dd[1].IsStarted);

			var events = sample.GetActiveEvents(obj).ToList();
			Assert.IsNotNull(events);
			Assert.AreEqual(5, events.Count);
		}

		private (SampleObj, Statechart<SampleObj>) Run_CanStartUpload()
		{
			var (obj, sample) = Run_CanMoveToCompositState();
			var upload = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Chart.Name == "Doc1" && o.Transition.Event.Name == "Upload");
			Assert.IsNotNull(upload);
			sample.OnEvent(upload, obj);
			Assert.AreEqual("Uploading", sample.CurrentState.Name);
			var doc1 = sample.ChartsByName["Doc1"];
			Assert.AreEqual("Uploaded", doc1.CurrentState.Name);

			return (obj, sample);
		}

		private (SampleObj, Statechart<SampleObj>) Run_CanHold()
		{
			var (obj, sample) = Run_CanStartUpload();

			var hold = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Hold");
			Assert.IsNotNull(hold);
			sample.OnEvent(hold, obj);
			Assert.AreEqual("OnHold", sample.CurrentState.Name);

			var events = sample.GetActiveEvents(obj).ToList();
			Assert.IsTrue(events.Exists(o => o.Transition.Event.Name == "Restart"));
			Assert.IsTrue(events.Exists(o => o.Transition.Event.Name == "Resume"));
			Assert.AreEqual(2, events.Count);
			return (obj, sample);
		}

		[TestMethod]
		public void F_CanHoldResumeTest()
		{
			var (obj, sample) = Run_CanHold();
			var resume = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Resume");
			Assert.IsNotNull(resume);
			sample.OnEvent(resume, obj);
			Assert.AreEqual("Uploading", sample.CurrentState.Name);

			var docs = sample.CurrentState.Charts;
			Assert.IsNotNull(docs);
			Assert.AreEqual(2, docs.Count);
			var dd = docs.ToArray();
			Assert.IsTrue(dd[0].IsStarted);
			Assert.IsTrue(dd[1].IsStarted);

			var events = sample.GetActiveEvents(obj).ToList();
			Assert.IsNotNull(events);
			Assert.AreEqual(5, events.Count);

			var doc1 = sample.ChartsByName["Doc1"];
			Assert.AreEqual("Uploaded", doc1.CurrentState.Name);
		}

		[TestMethod]
		public void G_CanReuploadTest()
		{
			var (obj, sample) = Run_CanStartUpload();
			var reupload = sample.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Reupload");
			Assert.IsNotNull(reupload);
			sample.OnEvent(reupload, obj);
			Assert.AreEqual("Uploading", sample.CurrentState.Name);

			var docs = sample.CurrentState.Charts;
			Assert.IsNotNull(docs);
			Assert.AreEqual(2, docs.Count);
			var dd = docs.ToArray();
			Assert.IsTrue(dd[0].IsStarted);
			Assert.IsTrue(dd[1].IsStarted);
			Assert.AreEqual("Waiting", dd[0].CurrentState.Name);
			Assert.AreEqual("Waiting", dd[1].CurrentState.Name);
		}

		private (SampleObj, Statechart<SampleObj>) Run_UploadDoc1()
		{
			var (obj, sample) = Run_CanStartUpload();

			var doc1 = sample.ChartsByName["Doc1"];
			Assert.AreEqual("Uploaded", doc1.CurrentState.Name);

			var ee = sample.GetActiveEvents(obj).Where(o => o.Transition.Event.Name == "Verify").ToList();
			Assert.AreEqual(1, ee.Count);
			sample.OnEvent(ee[0], obj);

			Assert.IsTrue(doc1.IsFinished);
			return (obj, sample);
		}

		[TestMethod]
		public void H_StateIsNotFinishedWhenSubchartsAreNotFinished()
		{
			var (obj, sample) = Run_UploadDoc1();
			Assert.AreEqual("Uploading", sample.CurrentState.Name);
			var ee = sample.GetActiveEvents(obj).ToList();
			Assert.AreEqual(4, ee.Count);
		}


		[TestMethod]
		public void I_StateIsFinishedWhenSubchartsAreFinished()
		{
			var (obj, sample) = Run_UploadDoc1();

			var doc2 = sample.ChartsByName["Doc2"];
			Assert.AreEqual("Waiting", doc2.CurrentState.Name);
			var e = doc2.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Upload");
			Assert.IsNotNull(e);
			sample.OnEvent(e, obj);
			Assert.AreEqual("Uploaded", doc2.CurrentState.Name);

			e = doc2.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Verify");
			Assert.IsNotNull(e);
			sample.OnEvent(e, obj);
			Assert.IsTrue(doc2.IsFinished);

			Assert.AreEqual("Completed", sample.CurrentState.Name);
			Assert.IsTrue(sample.IsFinished);
		}

		[TestMethod]
		public void J_CanSaveAndLoadStatechart()
		{
			var (obj, sample) = Run_UploadDoc1();
			sample.OnLoad += (o, c) => Array.ForEach(o.GetStates(), s => c.ChartsByName[s.Name].SetCurrentState(s.Value));
			sample.OnUpdate += (o, c) => o.SetStates(c.Charts.Select(s => (s.Name, s.CurrentState?.Name)));

			// Save current state
			sample.Update(obj);

			var x = sample.Charts.Select(o => o.CurrentState).ToArray();

			var doc2 = sample.ChartsByName["Doc2"];
			var e = doc2.GetActiveEvents(obj).FirstOrDefault(o => o.Transition.Event.Name == "Upload");
			Assert.IsNotNull(e);
			sample.OnEvent(e, obj);
			Assert.AreEqual("Uploaded", doc2.CurrentState.Name);
			var y = sample.Charts.Select(o => o.CurrentState).ToArray();

			CollectionAssert.AreNotEquivalent(x, y);

			sample.Load(obj);
			var z = sample.Charts.Select(o => o.CurrentState).ToArray();

			CollectionAssert.AreEquivalent(x, z);
		}

		public class SampleObj
		{
			public string?[] State { get; set; } = new string[3];
			private static string[] Names = new[] { "sample-1", "Doc1", "Doc2" };

			public void SetStates(IEnumerable<(string Name, string? Value)> states)
			{
				foreach (var state in states)
				{
					var i = Names.FindIndex(o => o == state.Name);
					if (i < 0)
						throw new ArgumentException($"Name not found '{state.Name}'");
					State[i] = state.Value;
				}
			}

			public (string Name, string? Value)[] GetStates()
			{
				return new[] { (Names[0], State[0]), (Names[1], State[1]), (Names[2], State[2]) };
			}

			public override string ToString() => String.Join(":", State);
		}
	}
}
