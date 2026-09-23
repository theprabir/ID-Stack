using System;
using System.Linq;
using IDStack.Services;
using IDStack.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="NavigationService"/> registry behavior.
    /// </summary>
    [TestClass]
    public class NavigationServiceTests
    {
        [TestMethod]
        public void NavigateTo_RegisteredKey_SetsCurrentViewModel()
        {
            var navigation = new NavigationService();
            var vm = new object();
            navigation.Register("Page", () => vm);

            navigation.NavigateTo("Page");

            Assert.AreEqual("Page", navigation.CurrentKey);
            Assert.AreSame(vm, navigation.CurrentViewModel);
        }

        [TestMethod]
        public void NavigateTo_UnknownKey_Throws()
        {
            var navigation = new NavigationService();
            Assert.ThrowsException<InvalidOperationException>(() => navigation.NavigateTo("Nope"));
        }

        [TestMethod]
        public void NavigateTo_NavigatingTwice_CachesViewModelInstance()
        {
            var navigation = new NavigationService();
            var callCount = 0;
            navigation.Register("Page", () => { callCount++; return new object(); });
            navigation.Register("Other", () => new object());

            navigation.NavigateTo("Page");
            navigation.NavigateTo("Other");
            navigation.NavigateTo("Page");

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void NavigateTo_SameKey_IsNoOp()
        {
            var navigation = new NavigationService();
            var raised = 0;
            navigation.NavigationChanged += (s, e) => raised++;
            navigation.Register("Page", () => new object());

            navigation.NavigateTo("Page");
            navigation.NavigateTo("Page");

            Assert.AreEqual(1, raised);
        }

        [TestMethod]
        public void NavigateTo_RaisesNavigationChanged()
        {
            var navigation = new NavigationService();
            navigation.Register("Page", () => new object());
            var raised = false;
            navigation.NavigationChanged += (s, e) => raised = true;

            navigation.NavigateTo("Page");

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void Register_NullFactory_Throws()
        {
            var navigation = new NavigationService();
            Assert.ThrowsException<ArgumentNullException>(() => navigation.Register("Page", null));
        }

        [TestMethod]
        public void NavigationKeys_AreUnique()
        {
            var keys = new[]
            {
                NavigationKeys.Home,
                NavigationKeys.TemplateEditor,
                NavigationKeys.TemplateLibrary,
                NavigationKeys.DataImport,
                NavigationKeys.BatchProcessing,
                NavigationKeys.Settings
            };

            Assert.AreEqual(keys.Length, keys.Distinct().Count());
        }
    }
}
