using Godot;
using System;
using ConnectFour.Core;


public partial class GameHandler : Node3D
{
    [Export] public Node3D Node3D;
    [Export] public MeshInstance3D FloatingDisc;

    //settings pour bien caler les jetons
    [Export] public Vector3 CellOrigin = new Vector3(-0.84f, 0.83f, 0); // position monde de la case (row=0,col=0) (en fait locale au Node3D)
    [Export] public Vector3 StepX = new Vector3(0.28f, 0, 0); // vecteur entre colonnes (local au Node3D)
    [Export] public Vector3 StepY = new Vector3(0, -0.325f, 0); // vecteur entre lignes (local au Node3D)
    [Export] public float HoverHeight = -5f; // hauteur du fantôme au-dessus de la rangée du haut
    [Export] public float DepthOffset = -0.01f; // ajustement axe z

    // setup des couleurs des joueurs
    [Export] public Color Player1Color = new Color(0.9f, 0.1f, 0.1f); // P1
    [Export] public Color Player2Color = new Color(1.0f, 0.9f, 0.1f); // P2 (jaune)

    //animations des jetons
    [Export] public float DropTime = 0.25f;
    [Export] public float DropBounce = 0.05f;
    [Export] public bool AnimateDrop = true; // si false on téléporte direct le jeton

    //orientation du plateau (normale locale qui pointe vers la caméra)
    [Export] public Vector3 BoardNormalLocal = new Vector3(0, 0, -1); // normale (local au Node3D)
    
    //ui
    [Export] public Label WinLabel;
    private bool _gameOver = false;

    //etat
    private int _hoverCol;
    private Node3D _floating;//jeton fantome pour savoir qui joue

    //call du core
    private Game _game;

    public override void _Ready()
    {
        GD.Print("Démarrage du jeu"); // doit s'afficher au lancement
        if (Node3D == null) Node3D = this;

        //instantiate des joueurs
        var p1 = new Player("P1", 'X');
        var p2 = new Player("P2", 'O');
        _game = new Game(p1, p2);

        //changement de couleur du jeton
        if (FloatingDisc != null)
        {
            // on relie _floating au mesh placé dans la scène
            _floating = FloatingDisc;

            // on duplique les matériaux pour qu'ils soient uniques (pas ceux partagés du GLB)
            var mesh = FloatingDisc.Mesh;
            if (mesh != null)
            {
                var surfCount = mesh.GetSurfaceCount();
                for (int i = 0; i < surfCount; i++)
                {
                    var active = FloatingDisc.GetActiveMaterial(i) as StandardMaterial3D;
                    var unique = active != null
                        ? (StandardMaterial3D)active.Duplicate()
                        : new StandardMaterial3D();

                    // sécurisation : on force en opaque pour éviter les artefacts de profondeur
                    unique.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                    unique.NoDepthTest = false;
                    unique.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.OpaqueOnly;

                    FloatingDisc.SetSurfaceOverrideMaterial(i, unique);
                }
            }
        }

        // on centre la colonne de survol au démarrage (confort visuel)
        _hoverCol = Board.Cols / 2;

        UpdateFloatingVisual();
        
        //on cache l'ui de victoire
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
        
        //on récup les touches
        if (Input.IsActionJustPressed("col_1")) _hoverCol = 0;
        if (Input.IsActionJustPressed("col_2")) _hoverCol = 1;
        if (Input.IsActionJustPressed("col_3")) _hoverCol = 2;
        if (Input.IsActionJustPressed("col_4")) _hoverCol = 3;
        if (Input.IsActionJustPressed("col_5")) _hoverCol = 4;
        if (Input.IsActionJustPressed("col_6")) _hoverCol = 5;
        if (Input.IsActionJustPressed("col_7")) _hoverCol = 6;

        _hoverCol = Mathf.Clamp(_hoverCol, 0, Board.Cols - 1);
        UpdateFloatingVisual();

        //on fait tomber le jeton dans la bonne colonne
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

    private void UpdateFloatingVisual() //pour changer la couleur du jeton
    {
        if (_floating == null) return;

        // position au-dessus de la rangée du haut
        var topWorld = GridTopWorld(_hoverCol);
        _floating.GlobalPosition = topWorld;

        SetDiscColor(_floating, GetColorForPlayer(_game.CurrentPlayer));
    }

    private void TryDrop(int col)
    {
        // on envoi la colonne choisie au core
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

    private void SpawnAndPlaceDisc(int col, int row, Color color) //création + anim/TP du jeton posé
    {
        // on duplique le mesh du jeton flottant (mêmes dimensions/matériaux)
        var discMesh = GetDiscMesh(_floating);
        if (discMesh == null) return;

        var disc = (MeshInstance3D)discMesh.Duplicate();
        disc.Name = $"Disc_{col}_{row}";
        AddChild(disc);

        // on rend les matériaux uniques pour éviter que tous les jetons partagent la même instance
        MakeMaterialsUnique(disc);

        // couleur du joueur qui vient de jouer
        SetDiscColor(disc, color);

        // positions calculées en monde (en utilisant le repère du Node3D)
        var from = GridTopWorld(col);                 // pile au-dessus de la colonne
        var to   = GridCellWorld(col, row);           // centre exact de la case

        if (!AnimateDrop)
        {
            // TP direct (pas de tween)
            disc.GlobalPosition = to;
            return;
        }

        // animation verticale uniquement (pas de changement de profondeur)
        disc.GlobalPosition = from;

        var up = GetUpWorld(); // direction verticale "locale" du plateau
        var tween = GetTree().CreateTween()
            .SetTrans(Tween.TransitionType.Sine)   // ease gravité plus crédible
            .SetEase(Tween.EaseType.In);

        tween.TweenProperty(disc, "global_position", to, DropTime);

        // petit rebond visuel si demandé
        if (DropBounce > 0f)
        {
            tween.Parallel().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(disc, "global_position", to + (-up * DropBounce), DropTime * 0.15f);
            tween.TweenProperty(disc, "global_position", to,                     DropTime * 0.10f);
        }
    }

    private Vector3 GridToWorld(int col, int row)
    {
        // calibration du plateau de jeu
        var t = Node3D != null ? Node3D.GlobalTransform : GlobalTransform;

        // conversion repère local → monde
        Vector3 originW = t.Origin + t.Basis * CellOrigin;
        Vector3 stepXW  = t.Basis * StepX;
        Vector3 stepYW  = t.Basis * StepY;

        return originW + stepXW * col + stepYW * row;
    }

    private Vector3 GridTopWorld(int col) //point au-dessus de la colonne (rangée du haut)
    {
        var baseTop = GridToWorld(col, Board.Rows - 1);
        return baseTop + GetUpWorld() * HoverHeight + GetNormalWorld() * DepthOffset;
    }

    private Vector3 GridCellWorld(int col, int row) //centre exact de la case
    {
        return GridToWorld(col, row) + GetNormalWorld() * DepthOffset;
    }

    private Vector3 GetNormalWorld() //normale en monde (stable, pas de "profondeur qui bouge")
    {
        var t = Node3D != null ? Node3D.GlobalTransform : GlobalTransform;
        return (t.Basis * BoardNormalLocal).Normalized();
    }

    private Vector3 GetUpWorld() //direction "vers le haut" du plateau (à partir de StepY)
    {
        var t = Node3D != null ? Node3D.GlobalTransform : GlobalTransform;
        return (t.Basis * StepY).Normalized();
    }

    private void SetDiscColor(Node3D disc, Color color) //logique pour changer la couleur du jeton
    {
        var mesh = disc.GetNodeOrNull<MeshInstance3D>(".")
                   ?? disc.GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        if (mesh == null) return;

        var mat = mesh.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
        if (mat == null)
        {
            // si jamais pas d'override, on en crée un
            mat = new StandardMaterial3D();
            // sécurisation : on force en opaque pour éviter les artefacts de profondeur
            mat.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
            mat.NoDepthTest = false;
            mat.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.OpaqueOnly;

            mesh.SetSurfaceOverrideMaterial(0, mat);
        }

        mat.AlbedoColor = color;
    }

    private Color GetColorForPlayer(Player p)
    {
        // on check qui joue pour connaitre la bonne couleur
        return (p == _game.Player1) ? Player1Color : Player2Color;
    }

    // --- helpers ajoutés ---

    private MeshInstance3D GetDiscMesh(Node3D from)
    {
        // petit helper pour récupérer le mesh du jeton (root ou enfant)
        return from.GetNodeOrNull<MeshInstance3D>(".")
            ?? from.GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
    }

    private void MakeMaterialsUnique(MeshInstance3D mi)
    {
        // on duplique tous les matériaux de surface pour casser les références partagées
        var mesh = mi.Mesh;
        if (mesh == null) return;

        int surfCount = mesh.GetSurfaceCount();
        for (int i = 0; i < surfCount; i++)
        {
            var active = mi.GetActiveMaterial(i) as StandardMaterial3D;
            var unique = active != null
                ? (StandardMaterial3D)active.Duplicate()
                : new StandardMaterial3D();

            // sécurisation : on force en opaque pour éviter les artefacts de profondeur
            unique.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
            unique.NoDepthTest = false;
            unique.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.OpaqueOnly;

            mi.SetSurfaceOverrideMaterial(i, unique);
        }
    }
}
