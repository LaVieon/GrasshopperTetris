﻿using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel.Special;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Tetris
{
    public class TetrisComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public TetrisComponent()
          : base("Tetris", "Tetris",
              "Description",
              "Qiu", "Tetris")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("control", "control", "", GH_ParamAccess.item, "");
            //pManager.AddBooleanParameter("restart", "restart", "", GH_ParamAccess.item,false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("string", "string", "", GH_ParamAccess.item);
        }
        private readonly Color[] tileImages = new Color[]
        {
            Color.FromArgb(50,0,0,0),
            Color.Cyan,
            Color.Blue,
            Color.Orange,
            Color.Yellow,
            Color.Green,
            Color.Purple,
            Color.Red
        };



        string output = String.Empty;
        public bool isPress = false;
        private GameState gameState = new GameState();
        private GH_Markup[,] imageControls;

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            imageControls = SetupGameCanvas(gameState.GameGrid);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected async override void SolveInstance(IGH_DataAccess DA)
        {
            string control=string.Empty;
            DA.GetData("control",ref control);
            //由于expire机制,所以这里需要先去除订阅,再增加事件订阅
            if (string.IsNullOrEmpty(control))
            {
                Instances.DocumentEditor.KeyDown -= this.GhKeydown;
                Instances.DocumentEditor.KeyDown += this.GhKeydown;
            }
            else if (control=="->")
            {
                gameState.MoveBlockRight();
            }

            await GameLoop();


            DA.SetData(0, output);
        }



        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Restart", (o, e) =>
            {
                gameState = new GameState();
                RemoveExpiredGrid(OnPingDocument());
                imageControls = SetupGameCanvas(gameState.GameGrid);

            });
        }
        public override void RemovedFromDocument(GH_Document document)
        {
            base.RemovedFromDocument(document);
            RemoveExpiredGrid(document);
        }

        private GH_Markup[,] SetupGameCanvas(GameGrid grid)
        {
            GH_Markup[,] imageControls = new GH_Markup[grid.Rows, grid.Columns];
            int cellSize = 50;
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Columns; c++)
                {
                    imageControls[r, c] = ConstructGrid(cellSize, 150 + c * cellSize, 150 + r * cellSize, tileImages[0]);
                }
            }
            var doc = OnPingDocument();
            foreach (var comp in imageControls)
            {
                doc.AddObject(comp, false);
            }
            return imageControls;
        }

        private void RemoveExpiredGrid(GH_Document doc)
        {
            //markup is not ActiveObject!
            //var obj=doc.ActiveObjects();
            List<IGH_DocumentObject> markupList = doc.Objects.Where(x => x.GetType() == typeof(GH_Markup)).ToList();
            doc.RemoveObjects(markupList, true);
        }

        private GH_Markup ConstructGrid(int cellSize, int pivotX, int pivotY, Color color, int width = 1)
        {
            List<Point3d> gridPts = new List<Point3d> { new Point3d(0, 0, 0), new Point3d(cellSize, 0, 0), new Point3d(cellSize, -cellSize, 0), new Point3d(0, -cellSize, 0) };
            gridPts.Add(gridPts[0]);

            var markGrid = new GH_Markup();
            markGrid.CreateAttributes();
            Polyline markLine = new Polyline(gridPts);
            markGrid.Marks.Add(markLine);
            markGrid.Attributes.Pivot = new PointF(0, 0);
            ChangeCellColor(ref markGrid, color, width);
            //var attributes = (GH_MarkupAttributes)markGrid.Attributes;
            //var prop= new GH_MarkupProperties();
            //prop.Width = width;
            //prop.Colour= color;
            //attributes.Properties= prop;
            markGrid.Attributes.Pivot = new PointF(pivotX, pivotY);

            return markGrid;
        }

        private async Task GameLoop()
        {
            Draw(gameState);

            while (!gameState.GameOver)
            {
                int delay = 500;
                await Task.Delay(delay);
                //后续加入暂停功能
                //if (gameState.Pause)
                //{
                //    await Task.Delay(2000);
                //}

                gameState.MoveBlockDown();
                Draw(gameState);
            }

            //GameOverMenu.Visibility = Visibility.Visible;
            //FinalScoreText.Text = $"Score: {gameState.Score}";
        }

        private void Draw(GameState gameState)
        {
            DrawGrid(gameState.GameGrid);
            //先画ghostBlock,这样GhostBlock会在Block的后面
            DrawGhostBlock(gameState.CurrentBlock);
            DrawBlock(gameState.CurrentBlock);

            //终于解决了画面更新的问题
            Instances.RedrawAll();

            //DrawNextBlock(gameState.BlockQueue);
            //ScoreText.Text = $"Score: {gameState.Score}";
        }

        private void DrawGhostBlock(Block block)
        {
            int dropDistance = gameState.BlockDropDistance();

            foreach (Position p in block.TilePositions())
            {
                //imageControls[p.Row + dropDistance, p.Column].Opacity = 0.25;
                //imageControls[p.Row + dropDistance, p.Column].Source = tileImages[block.Id];
                Color ghostColor = GetGhostColor(tileImages[block.Id]);
                bool isChangedColor = ChangeCellColor(ref imageControls[p.Row + dropDistance, p.Column], ghostColor);
                //if (isChangedColor)
                //{
                //    imageControls[p.Row + dropDistance, p.Column].Attributes.ExpireLayout();
                //}


            }
        }

        private Color GetGhostColor(Color color)
        {
            Color result = color;
            if (color.A > 100)
            {
                result = Color.FromArgb(100, color);
            }
            return result;
        }

        private void DrawBlock(Block block)
        {
            foreach (Position p in block.TilePositions())
            {
                //imageControls[p.Row, p.Column].Opacity = 1;
                //imageControls[p.Row, p.Column].Source = tileImages[block.Id];
                bool isChangedColor = ChangeCellColor(ref imageControls[p.Row, p.Column], tileImages[block.Id]);
                //if (isChangedColor)
                //{
                //    //imageControls[p.Row, p.Column].Attributes.PerformLayout();
                //    imageControls[p.Row, p.Column].Attributes.ExpireLayout();
                //    //imageControls[p.Row, p.Column].ExpireSolution(false);
                //    imageControls[p.Row, p.Column].ExpirePreview(true);

                //}

            }
        }

        private void DrawGrid(GameGrid grid)
        {
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Columns; c++)
                {
                    int id = grid[r, c];
                    bool isChangedColor = ChangeCellColor(ref imageControls[r, c], tileImages[id]);
                    //if (isChangedColor)
                    //{
                    //    //imageControls[r, c].Attributes.PerformLayout();
                    //    //imageControls[r, c].Attributes.ExpireLayout();
                    //}
                }
            }
        }

        private bool ChangeCellColor(ref GH_Markup cell, Color color, int width = 1)
        {
            bool isChanged = false;
            var attributes = (GH_MarkupAttributes)cell.Attributes;
            if (attributes.Properties.Colour != color)
            {
                isChanged = true;
                var prop = new GH_MarkupProperties();
                prop.Colour = color;
                if (color.A > 50)
                {
                    prop.Width = 10;
                }
                else
                {
                    prop.Width = 1;
                }
                attributes.Properties = prop;
            }
            return isChanged;
        }
        private void GhKeydown(object sender, KeyEventArgs e)
        {
            if (gameState.GameOver)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                    gameState.MoveBlockLeft();
                    break;
                case Keys.Right:
                    gameState.MoveBlockRight();
                    break;
                case Keys.Down:
                    gameState.MoveBlockDown();
                    break;
                case Keys.Up:
                    gameState.RotateBlockCW();
                    break;
                case Keys.Home:
                    gameState.RotateBlockCCW();
                    break;
                case Keys.End:
                    gameState.DropBlock();
                    break;
                case Keys.Delete:
                    gameState.Pause = !gameState.Pause;
                    break;
                default:
                    return;
            }
            Draw(gameState);
        }

        private void GhKeyIsString(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (gameState.GameOver)
            {
                return;
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
                // You can add image files to your project resources and access them like this:
                return Properties.Resources.T;
                //return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8275dc8c-915a-4b34-96a0-6490428bf3e1"); }
        }

    }
}
