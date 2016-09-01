using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrightstarDB.Tests.EntityFramework
{
    
    public class PropertyChangeNotificationTests
    {
        private readonly MyEntityContext _context;
        private readonly string _storeName;
        private readonly ICompany _company;
        private readonly IMarket _ftse;
        private readonly IMarket _nyse;
        private string _lastPropertyChanged;
        private readonly IFoafPerson _person;
        private NotifyCollectionChangedEventArgs _lastCollectionChangeEvent;

        public PropertyChangeNotificationTests()
        {
            _storeName = "PropertyChangeNotificationTests_" + DateTime.UtcNow.Ticks;
            _context = new MyEntityContext("type=embedded;storesDirectory=c:\\brightstar;storeName="+_storeName);
            _ftse = _context.Markets.Create();
            _nyse = _context.Markets.Create();
            _company = _context.Companies.Create();
            _company.Name = "Glaxo";
            _company.HeadCount = 20000;
            _company.PropertyChanged += HandlePropertyChanged;
            _person = _context.FoafPersons.Create();
            (_person.MboxSums as INotifyCollectionChanged).CollectionChanged += HandleCollectionChanged;
            _context.SaveChanges();
            _lastPropertyChanged = null;
        }

       
        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _lastCollectionChangeEvent = e;
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _lastPropertyChanged = e.PropertyName;
        }


        [Fact]
        public void TestStringPropertySetAndChanged()
        {
            _lastPropertyChanged = null;
            _company.TickerSymbol = "GLX";
            Assert.Equal("TickerSymbol", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.TickerSymbol = "GLXO";
            Assert.Equal("TickerSymbol", _lastPropertyChanged);
            
            _lastPropertyChanged = null;
            _company.TickerSymbol = "GLXO"; // No event fired when setting property to the same value
            Assert.Null(_lastPropertyChanged);
            
            _lastPropertyChanged = null;
            _company.TickerSymbol = null;
            Assert.Equal("TickerSymbol", _lastPropertyChanged);
            
            _lastPropertyChanged = null;
            _company.TickerSymbol = null;
            Assert.Null(_lastPropertyChanged);

            _lastPropertyChanged = null;
        }

        [Fact]
        public void TestIntegerPropertyChanged()
        {
            _lastPropertyChanged = null;
            _company.HeadCount = 25000;
            Assert.Equal("HeadCount", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.HeadCount = 25000;
            Assert.Null(_lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.HeadCount = 0;
            Assert.Equal("HeadCount", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.HeadCount = 0;
            Assert.Null(_lastPropertyChanged);

            _company.HeadCount = 15000;
            Assert.Equal("HeadCount", _lastPropertyChanged);
        }

        [Fact]
        public void TestRelatedEntityChanged()
        {
            _lastPropertyChanged = null;
            _company.ListedOn = _nyse;
            Assert.Equal("ListedOn", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.ListedOn = _nyse;
            Assert.Null(_lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.ListedOn = _ftse;
            Assert.Equal("ListedOn", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.ListedOn = null;
            Assert.Equal("ListedOn", _lastPropertyChanged);
        }

        [Fact]
        public void TestLiteralCollectionChangeEvents()
        {
            _lastCollectionChangeEvent = null;
            _person.MboxSums.Add("mboxsum1");
            Assert.NotNull(_lastCollectionChangeEvent);
            Assert.Equal(NotifyCollectionChangedAction.Add, _lastCollectionChangeEvent.Action);
            Assert.Equal(_lastCollectionChangeEvent.NewItems[0], "mboxsum1");

            _person.MboxSums.Add("mboxsum2");
            Assert.NotNull(_lastCollectionChangeEvent);
            Assert.Equal(NotifyCollectionChangedAction.Add, _lastCollectionChangeEvent.Action);
            Assert.Equal(_lastCollectionChangeEvent.NewItems[0], "mboxsum2");

            _person.MboxSums.Remove("mboxsum1");
            Assert.NotNull(_lastCollectionChangeEvent);
            Assert.Equal(NotifyCollectionChangedAction.Remove, _lastCollectionChangeEvent.Action);
            Assert.Equal(_lastCollectionChangeEvent.OldItems[0], "mboxsum1");

            _person.MboxSums.Clear();
            Assert.NotNull(_lastCollectionChangeEvent);
            Assert.Equal(NotifyCollectionChangedAction.Reset, _lastCollectionChangeEvent.Action);

            _lastCollectionChangeEvent = null;
            var friend = _context.FoafPersons.Create();
            (friend.KnownBy as INotifyCollectionChanged).CollectionChanged += HandleCollectionChanged;
            _person.Knows.Add(friend);
            Assert.NotNull(_lastCollectionChangeEvent);
            Assert.Equal(NotifyCollectionChangedAction.Add, _lastCollectionChangeEvent.Action);
            Assert.Equal(_person, _lastCollectionChangeEvent.NewItems[0]);

            _lastCollectionChangeEvent = null;
            (friend.KnownBy as INotifyCollectionChanged).CollectionChanged -= HandleCollectionChanged;
            (_person.Knows as INotifyCollectionChanged).CollectionChanged += HandleCollectionChanged;
            _person.Knows.Remove(friend);
            Assert.NotNull(_lastCollectionChangeEvent);
            Assert.Equal(NotifyCollectionChangedAction.Remove, _lastCollectionChangeEvent.Action);
            Assert.Equal(friend, _lastCollectionChangeEvent.OldItems[0]);

        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
