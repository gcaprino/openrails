using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices; // for ComVisible

namespace Orts.Viewer3D.TrainDirector
{
    public partial class TDConnectorView : Form
    {
        TDController controller;
        string TDserverUrl;

        public TDConnectorView(TDController c)
        {
            controller = c;
            InitializeComponent();
            controller.SetForm(this);
            TDUrl.Text = "http://127.0.0.1:8080/TDWebUI/layout.html";

            LayoutBrowser.ObjectForScripting = new ScriptManager(this);
        }

        private void TDUrl_TextChanged(object sender, EventArgs e)
        {

        }

        private void ConnectTDbutton_Click(object sender, EventArgs e)
        {
            TDserverUrl = TDUrl.Text;
            controller.SetTDUrl(TDUrl.Text);
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            LayoutBrowser.Navigate(new Uri(TDserverUrl));
            controller.GetTDState();
        }

        // From: https://www.codeproject.com/tips/130267/call-a-c-method-from-javascript-hosted-in-a-webbro
        // This nested class must be ComVisible for the JavaScript to be able to call it.
        [ComVisible(true)]
        public class ScriptManager
        {
            private TDConnectorView mForm;

            public ScriptManager(TDConnectorView form)
            {
                mForm = form;
            }

            // Player clicked on the layout in the canvas at cell x,y
            // mods |= 1 if right button, |= 2 if ctrl, |= 4 if shift, |= 8 if alt
            public void OnCanvasClick(int x, int y, int mods)
            {
                mForm.controller.OnCellClick(x, y, mods);
                string req = "http://127.0.0.1:8080/poll/?parts=layout";
                var responseString = TDController.client.GetStringAsync(req).Result;
                mForm.LayoutBrowser.Document.InvokeScript("pollLayout", new string[] { responseString });
            }
        }

        private void LayoutBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void TDConnectorView_Load(object sender, EventArgs e)
        {

        }
    }
}
