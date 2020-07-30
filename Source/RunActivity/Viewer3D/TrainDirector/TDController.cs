using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.Signalling;

namespace Orts.Viewer3D.TrainDirector
{
    public class TDController
    {
        TDModel Model;
        string TDurl;
        TDConnectorView Form;
        Simulator simulator;
        public static readonly HttpClient client = new HttpClient();

        public TDController(TDModel m, Simulator s)
        {
            Model = m;
            simulator = s;
        }

        public void SetTDUrl(string url)
        {
            TDurl = url;
        }

        public void SetForm(TDConnectorView f)
        {
            Form = f;
        }

        public void GetTDState()
        {
            try
            {
                var responseString = client.GetStringAsync("http://127.0.0.1:8080/poll/?parts=layout").Result;
                TDLayout newState = JsonSerializer.Deserialize<TDLayout>(responseString);
                if (Model.Layout == null)
                {
                    Model.Layout = newState;
                }
                else
                {
                    // scan newState for switches, signals
                    // compare switches/signals state with last one in Model.Layout
                    // send Messages to OR to reflect the new state
                    TDElement curItem;

                    foreach (var newItem in newState.layout)
                    {
                        switch(newItem.type)
                        {
                            case TDModel.SIGNAL:
                                curItem = Model.FindElementAt(newItem.x, newItem.y);
                                if (curItem.shape != newItem.shape)
                                {
                                    SignalItem si;
                                    // send to OR signal has changed message
                                    foreach (var item in simulator.TDB.TrackDB.TrItemTable)
                                    {
                                        if (item.ItemType == TrItem.trItemType.trSIGNAL)
                                        {
                                            if (item is SignalItem)
                                            {
                                                si = item as SignalItem;
                                                SignalObject signal = null; // signalObjectTable[si.SigObj]
                                                // TODO: how to map TD's signal aspects/shapes to OR's limited types?
                                                int type = 0;
                                                switch (type)
                                                {
                                                    case 0:
                                                        signal.clearHoldSignalDispatcher();
                                                        break;
                                                    case 1:
                                                        signal.requestHoldSignalDispatcher(true);
                                                        break;
                                                    case 2:
                                                        signal.holdState = SignalObject.HoldState.ManualApproach;
                                                        foreach (var sigHead in signal.SignalHeads)
                                                        {
                                                            var drawstate1 = sigHead.def_draw_state(MstsSignalAspect.APPROACH_1);
                                                            var drawstate2 = sigHead.def_draw_state(MstsSignalAspect.APPROACH_2);
                                                            var drawstate3 = sigHead.def_draw_state(MstsSignalAspect.APPROACH_3);
                                                            if (drawstate1 > 0) { sigHead.state = MstsSignalAspect.APPROACH_1; }
                                                            else if (drawstate2 > 0) { sigHead.state = MstsSignalAspect.APPROACH_2; }
                                                            else { sigHead.state = MstsSignalAspect.APPROACH_3; }
                                                            sigHead.draw_state = sigHead.def_draw_state(sigHead.state);
                                                        }
                                                        break;
                                                    case 3:
                                                        signal.holdState = SignalObject.HoldState.ManualPass;
                                                        foreach (var sigHead in signal.SignalHeads)
                                                        {
                                                            sigHead.SetLeastRestrictiveAspect();
                                                            sigHead.draw_state = sigHead.def_draw_state(sigHead.state);
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    curItem.shape = newItem.shape;
                                }
                                break;
                            case TDModel.SWITCH:
                                curItem = Model.FindElementAt(newItem.x, newItem.y);
                                if (curItem.switched != newItem.switched)
                                {
                                    var nodes = simulator.TDB.TrackDB.TrackNodes;
                                    for (int i = 0; i < nodes.Length; i++)
                                    {
                                        TrackNode currNode = nodes[i];

                                        if (currNode != null)
                                        {
                                            if (currNode.TrEndNode)
                                            {
                                                //buffers.Add(new PointF(currNode.UiD.TileX * 2048 + currNode.UiD.X, currNode.UiD.TileZ * 2048 + currNode.UiD.Z));
                                            }
                                            else if (currNode.TrVectorNode != null)
                                            {

                                                if (currNode.TrVectorNode.TrVectorSections.Length > 1)
                                                {
                                                    //                                                    AddSegments(segments, currNode, currNode.TrVectorNode.TrVectorSections, ref minX, ref minY, ref maxX, ref maxY, simulator);
                                                }
                                                else
                                                {
                                                    TrVectorSection s = currNode.TrVectorNode.TrVectorSections[0];
                                                    foreach (TrPin pin in currNode.TrPins)
                                                    {
                                                    }
                                                }
                                            }
                                            else if (currNode.TrJunctionNode != null)
                                            {
                                                // throw switch
                                                switch (newItem.switched)
                                                {
                                                    case 0:
                                                        Program.Simulator.Signals.RequestSetSwitch(currNode, (int)0);
                                                        //sw.SelectedRoute = (int)switchPickedItem.main;
                                                        break;
                                                    case 1:
                                                        Program.Simulator.Signals.RequestSetSwitch(currNode, 1);
                                                        //sw.SelectedRoute = 1 - (int)switchPickedItem.main;
                                                        break;
                                                }
                                            }
                                        }

                                        // send to OR switch change message
                                        TrackNode tn = null; // FindTrackNode(curItem.name)
                                                             //Program.Simulator.Signals.RequestSetSwitch(tn, (int)switchPickedItem.main);

                                        curItem.switched = newItem.switched;
                                        //ColorTDTrack(29, 16, 2);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch
            {

            }
        }

        /*
          	color_black = 0;
	        color_white = 1;
	        color_green = 2;
	        color_yellow = 3;
	        color_red = 4;
	        color_orange = 5;
	        color_brown = 6;
	        color_gray = 7;
	        color_lightgray = 8;
	        color_darkgray = 9;
	        color_blue = 10;
	        color_cyan = 11;
            color_magenta = 12;
         */
        public void ColorTDTrack(int x, int y, int color)
        {
            string req = String.Format("/war/do?color%20{0}%20{1}%20{2}", x, y, color);
            var responseString = client.GetStringAsync("http://127.0.0.1:8080" + req).Result;
        }

        public void OnCellClick(int x, int y, int mods)
        {
            TDElement elem = Model.FindElementAt(x, y);
            if (elem != null)
            {
                switch (elem.type)
                {
                    //                    case TDModel.TRACK:
                    case TDModel.SWITCH:
                    case TDModel.SIGNAL:
                        // mods |= 1 if right button, |= 2 if ctrl, |= 4 if shift, |= 8 if alt
                        string req = "/war/click?x=" + x + "&y=" + y;
                        req += "&shift=" + ((mods & 8) != 0 ? "1" : "0");
                        req += "&alt=" + ((mods & 4) != 0 ? "1" : "0");
                        req += "&ctrl=" + ((mods & 2) != 0 ? "1" : "0");
                        req += "&btn=" + ((mods & 1) != 0 ? "1" : "0");
                        var responseString = client.GetStringAsync("http://127.0.0.1:8080" + req).Result;
                        break;
                }
                // send click message to TD
            }
        }
    }
}
