using Grasshopper;
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
        private GH_Markup[,] nextBlockCells;
        private GH_Scribble nextText;
        private GH_Scribble scoreText;
        public async override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            imageControls = SetupGameCanvas(gameState.GameGrid);
            await GameLoop();
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string control = string.Empty;
            DA.GetData("control", ref control);
            //由于expire机制,所以这里需要先去除订阅,再增加事件订阅
            if (control == "")
            {
                Message = "Inside Control Mode";
                Instances.DocumentEditor.KeyDown -= this.GhKeydown;
                Instances.DocumentEditor.KeyDown += this.GhKeydown;
            }
            else
            {
                Message = "Remote Control Mode";
                Instances.DocumentEditor.KeyDown -= this.GhKeydown;
                ParseControl(control);
            }

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
            var doc = OnPingDocument();
            //设置网格
            GH_Markup[,] imageControls = new GH_Markup[grid.Rows, grid.Columns];
            int cellSize = 50;
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Columns; c++)
                {
                    imageControls[r, c] = ConstructGrid(cellSize, 150 + c * cellSize, 150 + r * cellSize, tileImages[0]);
                }
            }
            foreach (var comp in imageControls)
            {
                doc.AddObject(comp, false);
            }

            //设置分数
            scoreText = new GH_Scribble();
            scoreText.CreateAttributes();
            scoreText.Attributes.Pivot = new PointF(300, 100);
            scoreText.Font = GH_FontServer.NewFont(GH_FontServer.Script.FontFamily, 40f, FontStyle.Bold);
            scoreText.NickName = "ScoreText";
            scoreText.Text = "Score : " + gameState.Score.ToString();
            doc.AddObject(scoreText, false);

            //设置NextBlockCells
            nextBlockCells = new GH_Markup[4, 4];
            for (int r = 0; r < nextBlockCells.GetLength(0); r++)
            {
                for (int c = 0; c < nextBlockCells.GetLength(1); c++)
                {
                    nextBlockCells[r, c] = ConstructGrid(cellSize, 750 + c * cellSize, 450 + r * cellSize, tileImages[0]);
                }
            }
            foreach (var comp in nextBlockCells)
            {
                doc.AddObject(comp, false);
            }


            //设置Next
            nextText = new GH_Scribble();
            nextText.CreateAttributes();
            nextText.Attributes.Pivot = new PointF(800, 400);
            nextText.Font = GH_FontServer.NewFont(GH_FontServer.Script.FontFamily, 40f, FontStyle.Bold);
            nextText.NickName = "NextText";
            nextText.Text = "Next";
            doc.AddObject(nextText, false);

            return imageControls;
        }

        private void RemoveExpiredGrid(GH_Document doc)
        {
            //markup is not ActiveObject!
            //var obj=doc.ActiveObjects();
            List<IGH_DocumentObject> markupList = doc.Objects.Where(x => x.GetType() == typeof(GH_Markup) || x.GetType() == typeof(GH_Scribble)).ToList();
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

            DrawNextBlock(gameState.BlockQueue);

            scoreText.Text = "Score : " + gameState.Score.ToString();
            scoreText.Attributes.ExpireLayout();
            scoreText.OnDisplayExpired(true);

            //终于解决了画面更新的问题
            Instances.RedrawAll();
        }

        private void DrawNextBlock(BlockQueue blockQueue)
        {
            //setup next cells
            for (int r = 0; r < nextBlockCells.GetLength(0); r++)
            {
                for (int c = 0; c < nextBlockCells.GetLength(1); c++)
                {
                    bool isChangedColor = ChangeCellColor(ref nextBlockCells[r, c], tileImages[0]);
                }
            }
            //Draw next block
            Block next = blockQueue.NextBlock;
            var test = next.TilePositionsInNext();
            foreach (Position p in next.TilePositionsInNext())
            {
                bool isChangedColor = ChangeCellColor(ref nextBlockCells[p.Row, p.Column], tileImages[next.Id]);
            }
        }

        private void DrawGhostBlock(Block block)
        {
            int dropDistance = gameState.BlockDropDistance();

            foreach (Position p in block.TilePositions())
            {
                Color ghostColor = GetGhostColor(tileImages[block.Id]);
                bool isChangedColor = ChangeCellColor(ref imageControls[p.Row + dropDistance, p.Column], ghostColor);
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
                bool isChangedColor = ChangeCellColor(ref imageControls[p.Row, p.Column], tileImages[block.Id]);
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
        private void ParseControl(string control)
        {
            if (gameState.GameOver)
            {
                return;
            }
            switch (control)
            {
                case "Left":
                    gameState.MoveBlockLeft();
                    break;
                case "Right":
                    gameState.MoveBlockRight();
                    break;
                case "Down":
                    gameState.MoveBlockDown();
                    break;
                case "Up":
                    gameState.RotateBlockCW();
                    break;
                case "Home":
                    gameState.RotateBlockCCW();
                    break;
                case "Drop":
                    gameState.DropBlock();
                    break;
                //case Keys.Delete:
                //    gameState.Pause = !gameState.Pause;
                //    break;
                default:
                    return;
            }
            Draw(gameState);
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
