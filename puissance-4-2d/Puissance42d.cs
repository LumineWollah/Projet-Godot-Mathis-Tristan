using Godot;
using System;
using ConnectFour.Core;

public partial class Puissance42d : Node2D
{

    [Export] public Node2D BoardNode;
    [Export] public Sprite2D FloatingDisc;

    // settings pour bien caler les jetons
    [Export] public Vector2 CellOrigin = new Vector2(-84f, 83f);  // position d'origine en bas a gauche
    [Export] public Vector2 StepX = new Vector2(28f, 0f);         // distance entre colonnes
    [Export] public Vector2 StepY = new Vector2(0f, -32.5f);      // distance entre lignes
    [Export] public float HoverHeight = -50f;                     // décalage au dessus du plateau
    [Export] public int   GhostZIndex = 100;                      // z-index du jeton flottant
    [Export] public int   DiscZIndex  = 10;                       // z-index des jetons posés

    // setup des couleurs des joueurs
    [Export] public Color Player1Color = new Color(0.9f, 0.1f, 0.1f);
    [Export] public Color Player2Color = new Color(1.0f, 0.9f, 0.1f);

    // animations des jetons
    [Export] public float DropTime = 0.25f;
    [Export] public float DropBounce = 0.05f;
    [Export] public bool  AnimateDrop = true; // si false on téléporte direct le jeton

    // ui
    [Export] public Label WinLabel;
    private bool _gameOver = false;

    // état
    private int _hoverCol;
    private Sprite2D _floating; // jeton fantôme pour savoir qui joue

    // call du core
    private Game _game;

    public override void _Ready()
    {
        GD.Print("Démarrage du jeu 2D"); // doit s'afficher au lancement
        if (BoardNode == null) BoardNode = this;

        // instantiate des joueurs
        var p1 = new Player("P1", 'X');
        var p2 = new Player("P2", 'O');
        _game = new Game(p1, p2);

        // configuration du jeton fantôme
        if (FloatingDisc != null)
        {
            _floating = FloatingDisc;
            _floating.Visible = true;
            _floating.SelfModulate = new Color(1f, 1f, 1f);
            _floating.ZIndex = GhostZIndex;
        }

        // on centre la colonne de survol au démarrage
        _hoverCol = Board.Cols / 2;

        UpdateFloatingVisual();

        // on cache l'ui de victoire
        if (WinLabel != null)
        {
            WinLabel.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        // pour relancer une partie
        if (Input.IsActionJustPressed("Restart game"))
        {
            GetTree().ReloadCurrentScene();
            return;
        }

        // bloquage des touches si la partie est terminée
        if (_gameOver) return;

        // on récup les touches (1..7)
        if (Input.IsActionJustPressed("col_1")) _hoverCol = 0;
        if (Input.IsActionJustPressed("col_2")) _hoverCol = 1;
        if (Input.IsActionJustPressed("col_3")) _hoverCol = 2;
        if (Input.IsActionJustPressed("col_4")) _hoverCol = 3;
        if (Input.IsActionJustPressed("col_5")) _hoverCol = 4;
        if (Input.IsActionJustPressed("col_6")) _hoverCol = 5;
        if (Input.IsActionJustPressed("col_7")) _hoverCol = 6;

        _hoverCol = Mathf.Clamp(_hoverCol, 0, Board.Cols - 1);
        UpdateFloatingVisual();

        // on fait tomber le jeton dans la bonne colonne
        if (Input.IsActionJustPressed("col_1") ||
            Input.IsActionJustPressed("col_2") ||
            Input.IsActionJustPressed("col_3") ||
            Input.IsActionJustPressed("col_4") ||
            Input.IsActionJustPressed("col_5") ||
            Input.IsActionJustPressed("col_6") ||
            Input.IsActionJustPressed("col_7"))
        {
            TryDrop(_hoverCol);
        }
    }

    private void UpdateFloatingVisual() // pour changer la couleur et la position du jeton fantôme
    {
        if (_floating == null) return;

        // position au-dessus de la rangée du haut
        var topWorld = GridTopWorld(_hoverCol);
        _floating.GlobalPosition = topWorld;

        SetDiscColor(_floating, GetColorForPlayer(_game.CurrentPlayer));
    }

    private void TryDrop(int col)
    {
        // on envoie la colonne choisie au core
        if (!_game.Board.PlaceDisc(col, _game.CurrentPlayer.Disc, out var rowPlaced))
        {
            GD.PushWarning($"Colonne {col + 1} pleine.");
            return;
        }

        // on crée le jeton 'posé' et on l'anime jusqu'à la case
        SpawnAndPlaceDisc(col, rowPlaced, GetColorForPlayer(_game.CurrentPlayer));

        // on check si le joueur gagne
        var win = Rules.CheckWin(_game.Board, rowPlaced, col);

        if (win)
        {
            GD.Print($"🎉 {_game.CurrentPlayer.Name} a gagné !");
            _gameOver = true;

            // actualisation de l'ui
            if (WinLabel != null)
            {
                WinLabel.Text = $"🎉 {_game.CurrentPlayer.Name} a gagné !";
                WinLabel.Visible = true;

                // petite fondue
                var panel = WinLabel.GetParentOrNull<CanvasItem>();
                if (panel != null)
                {
                    panel.Modulate = new Color(panel.Modulate.R, panel.Modulate.G, panel.Modulate.B, 0f);
                    var t = GetTree().CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                    t.TweenProperty(panel, "modulate:a", 1f, 0.35f);
                }
            }
        }
        else
        {
            // si pas de victoire on relance
            _game.SwitchTurn();
            UpdateFloatingVisual();
        }
    }

    private void SpawnAndPlaceDisc(int col, int row, Color color)
    {
        // on duplique le sprite du jeton flottant
        var discSprite = GetDiscSprite(_floating);
        if (discSprite == null) return;

        var disc = (Sprite2D)discSprite.Duplicate();
        disc.Name = $"Disc_{col}_{row}";
        disc.Visible = true;
        disc.SelfModulate = Colors.White;
        disc.Modulate = color;
        disc.ZIndex = DiscZIndex;

        //copie uniquement l’échelle du jeton flottant
        disc.Scale = _floating.Scale;

        var parent = _floating.GetParent();
        parent.AddChild(disc);
        disc.GlobalScale = _floating.GlobalScale;
        // positions calculées en monde
        var from = GridTopWorld(col);       // pile au-dessus de la colonne
        var to   = GridCellWorld(col, row); // centre exact de la case

        if (!AnimateDrop)
        {
            // TP direct
            disc.GlobalPosition = to;
            return;
        }

        // animation verticale uniquement
        disc.GlobalPosition = from;

        var up = GetUpWorld();

        var tween = GetTree().CreateTween()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);

        tween.TweenProperty(disc, "global_position", to, DropTime);

        // petit rebond visuel si demandé
        if (DropBounce > 0f)
        {
            tween.Parallel().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(disc, "global_position", to + (-up * DropBounce), DropTime * 0.15f);
            tween.TweenProperty(disc, "global_position", to,                      DropTime * 0.10f);
        }
    }
    
    private Vector2 GridToWorld(int col, int row)
    {
        // calibration du plateau de jeu
        var t = BoardNode != null ? BoardNode.GlobalTransform : GlobalTransform;
        // position locale
        Vector2 local = CellOrigin + StepX * col + StepY * row;
        return t.Origin + t.BasisXform(local);
    }

    private Vector2 GridTopWorld(int col) // point au-dessus de la colonne (rangée du haut)
    {
        var baseTop = GridToWorld(col, Board.Rows - 1);
        return baseTop + GetUpWorld() * HoverHeight;
    }

    private Vector2 GridCellWorld(int col, int row) // centre exact de la case
    {
        return GridToWorld(col, row);
    }

    private Vector2 GetUpWorld()
    {
        var t = BoardNode != null ? BoardNode.GlobalTransform : GlobalTransform;
        var up = t.BasisXform(StepY).Normalized();
        return up;
    }
    
    private void SetDiscColor(Sprite2D disc, Color color) // logique pour changer la couleur du jeton
    {
        disc.Modulate = color;
    }

    private Color GetColorForPlayer(Player p)
    {
        // on check qui joue pour connaitre la bonne couleur
        return (p == _game.Player1) ? Player1Color : Player2Color;
    }

    private Sprite2D GetDiscSprite(Node from)
    {
        // petit helper pour récupérer le sprite du jeton
        if (from is Sprite2D s) return s;
        return from.GetNodeOrNull<Sprite2D>("Sprite2D");
    }
}
