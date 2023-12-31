﻿using TravelApp.Model;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TravelApp.Event;
using FindDeals;
using Newtonsoft.Json;
using unirest_net.http;
using System.Collections.ObjectModel;
using Prism.Commands;
using System.Threading;

namespace TravelApp.ViewModels
{
    public class FindDealsViewModel : ViewModelBase, IPageViewModel
    {

        private IEventAggregator _eventAggregator;


        public int Hei = 0;

        public int hei
        {
            get { return Hei; }
            set
            {
                Hei = value;
                OnPropertyChanged();
            }
        }

        public FindDealsViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            //here
           
            _eventAggregator.GetEvent<PostEvent>().Subscribe(OnPostedEvent);
            Itineraries = new ObservableCollection<Itinerary>();
            Infos = new ObservableCollection<Useful>();
            hei = 40;
        }

        private bool OnGoCanExecute()
        {
            return true;
        }
        public delegate void GoSuccessfullyHandler();
        public event GoSuccessfullyHandler GoSuccessfullyEvent;
      

        public Info res { get; set; }
        public string ms { get; set; }
        public ObservableCollection<Itinerary> Itineraries { get; set; }
        public ObservableCollection<Useful> Infos { get; set; }
        private Useful _selectedIt;
        public Useful SelectedIt
        {
            get { return _selectedIt; }
            set
            {
                _selectedIt = value;
                _selectedIt.PricingOptions = res.Itineraries.First(itemm => itemm.OutboundLegId == _selectedIt.OutboundLegId).PricingOptions;
                OnPropertyChanged();
                if (_selectedIt != null)
                {
                    _eventAggregator.GetEvent<SelectEvent>().Publish(new SelectEventArgs
                    { sl=_selectedIt,
                        ag= res.Agents
                });
                    _eventAggregator.GetEvent<SendToPlanEvent>().Publish(new PlanFlight
                    {   city=_selectedIt.Destcity,
                        airport =_selectedIt.DestPlace ,
                        arriveDate= _selectedIt.ArrivalDate,
                        arriveTime = _selectedIt.Arrival
                    });
                GoSuccessfullyEvent();
                }
            }
        }
        public Thread thread { get; set; }
        public string PlInfo { get; set; }
        public string travInfo { get; set; }
        public string classInfo { get; set; }
        public string dateInfo { get; set; }
        private void OnPostedEvent(PostEventAggregators obj)
        {
            //{obj.basicflightinfo.Passengers.Count.ToString()}
            ms = obj.ms;
            PlInfo = $"{ obj.basicflightinfo.FromLocation} - {obj.basicflightinfo.ToLocation} ";
            travInfo = $"1 passenger";
            dateInfo = $"{obj.basicflightinfo.StartTime.ToString().Split(' ').First()} \t\b {obj.basicflightinfo.EndTime.ToString().Split(' ').First()}";

            Task.Factory.StartNew(load).ContinueWith(task =>
            {
                hei = 0;
            });
            thread = new Thread(() => { load(); });
            //thread.IsBackground = false;
            thread.Start();
            
            //load();
        }
        private void load()
        {
            var responsedhy = Unirest.get("https://skyscanner-skyscanner-flight-search-v1.p.rapidapi.com/apiservices/pricing/uk2/v1.0/%2F" + ms + "?pageIndex=0&pageSize=10")
.header("X-RapidAPI-Key", "107dc24922mshbd9bd597451997fp19a55fjsnc869fe1003a4")
.asJson<string>();




            res = JsonConvert.DeserializeObject<Info>(responsedhy.Body);

            Console.WriteLine(res.Itineraries[0].PricingOptions[0].Price);

            int ag = res.Itineraries[0].PricingOptions[0].Agents[0];
            foreach (var item in res.Itineraries)
            {
                Useful newo = new Useful();
                Itineraries.Add(item);
                newo.Price = $"{item.PricingOptions[0].Price}$";
                newo.OutboundLegId = item.OutboundLegId;

                var value = res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).OriginStation;
                var value2 = res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).DestinationStation;
                newo.Duration =$"{ res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).Duration / 60}h { res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).Duration%60} ";
                newo.Deals = $"{item.PricingOptions.Count}  deals";
                var photo = res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).Carriers[0];
                newo.Photo = res.Carriers.First(itemm => itemm.Id == photo).ImageUrl;
                if (res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).SegmentIds.Count == 2)
                    newo.Mode = "Direct";
                else newo.Mode = $"{(res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).SegmentIds.Count - 2).ToString()} stop";

                newo.Departuretime = res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).Departure.Split('T').Last(); ;
                newo.Departuredate = res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).Departure.Split('T').First(); ;
                newo.Arrival = res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).Arrival.Split('T').Last();
                newo.ArrivalDate = res.Legs.First(itemm => itemm.Id == newo.OutboundLegId).Arrival.Split('T').First();
                newo.OriginPlace = res.Places.First(itemm => itemm.Id == value).Code;
                newo.DestPlace = res.Places.First(itemm => itemm.Id == value2).Code;
                newo.Destcity = res.Places.First(itemm => itemm.Id == value2).Name;
                Application.Current.Dispatcher.Invoke(new Action(() => { Infos.Add(newo); }));
                //Infos.Add(newo);

            }
        }
    }
}
