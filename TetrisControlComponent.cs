using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Tetris
{
    public class TetrisControlComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public TetrisControlComponent()
          : base("TetrisControl", "TC",
              "Description",
              "Qiu", "Tetris")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("string", "string", "Parse Keyboard input.\nIt can be understood as the interface left by the extensibility", GH_ParamAccess.item);
        }
        string output = String.Empty;
        public bool isPress = false;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Instances.DocumentEditor.KeyDown -= this.GhKeydown;
            Instances.DocumentEditor.KeyDown += this.GhKeydown;


            DA.SetData(0, output);
        }

        private void GhKeydown(object sender, KeyEventArgs e)
        {

            this.isPress = false;
            //var controlKeys=new List<Keys> { Keys.Left,Keys.Right,Keys.Down,Keys.Up};
            //bool flag2 = e.Control && e.KeyCode == Keys.Right;
            if (e.Control && e.KeyCode!=Keys.ControlKey)
            {
                switch (e.KeyCode)
                {
                    case Keys.Left:
                        output = "Left";
                        break;
                    case Keys.Right:
                        output = "Right";
                        break;
                    case Keys.Down:
                        output = "Down";
                        break;
                    case Keys.Up:
                        output = "Up";
                        break;
                    case Keys.Home:
                        output = "Home";
                        break;
                    case Keys.End:
                        output = "Drop";
                        break;
                    default:
                        return;
                }
                this.isPress = true;
                ExpireSolution(true);
            }
            if (e.Control && e.Alt)
            {
                output = String.Empty;
            }

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8275dc8c-915a-4b34-96a0-6490428bf3e2"); }
        }

    }
}
