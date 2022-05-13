using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;


//TODO: Continuer d'implementer en fonction des besoins recurents des fenetre qui en dependent


namespace PulseEngine
{

    #region Enums #################################################################################

    /// <summary>
    /// Les differents modes dans lesquels une fenetre d'editeur peut etre ouverte.
    /// </summary>
    public enum EditorMode
    {
        Edition, Selection, DataEdition, Preview, NodeGraph
    }

    #endregion

    /// <summary>
    /// La classe de base pour tous les editeurs du Moteur.
    /// </summary>
    public abstract class PulseEditor<T> : EditorWindow where T : ScriptableResource
    {
        #region Constants #################################################################

        /// <summary>
        /// Le nombre de charactere maximal d'une liste.
        /// </summary>
        protected const int LIST_MAX_CHARACTERS = 20;

        /// <summary>
        /// Le nombre de charactere maximal d'une d'un champ texte.
        /// </summary>
        protected const int FIELD_MAX_CHARACTERS = 50;

        #endregion

        #region Protected Atrributes ##########################################################################

        /// <summary>
        /// Le mode dans lequel la fenetre a ete ouverte.
        /// </summary>
        protected EditorMode currentEditorMode;

        /// <summary>
        /// l'asset original.
        /// </summary>
        protected T originalAsset;

        /// <summary>
        /// La liste des datas en cours de modification.
        /// </summary>
        protected List<T> dataList = new List<T>();

        /// <summary>
        /// La data en cours de modification.
        /// </summary>
        protected T data;

        /// <summary>
        /// l'index de l'asset dans all assets.
        /// </summary>
        protected int selectAssetIndex;

        /// <summary>
        /// l'index de la data dans la liste des datas.
        /// </summary>
        protected int selectDataIndex = -1;

        /// <summary>
        /// L'id de la data a modifier en mode modification
        /// </summary>
        protected int dataID;

        /// <summary>
        /// Le parametre pour retrouver un asset, souvent le scope auquel il appartient
        /// </summary>
        protected int assetMainFilter;

        /// <summary>
        /// Le parametre pour retrouver un asset, souvent la zone a laquelle il appartient.
        /// </summary>
        protected int assetLocalFilter;

        /// <summary>
        /// The copied object on the clipboard.
        /// </summary>
        protected dynamic clipBoard;

        #endregion

        #region Private Atrributes ##########################################################################

        /// <summary>
        /// La liste des pths des datas a supprimer a la sauvegarde.
        /// </summary>
        protected List<string> delAssetPath = new List<string>();

        /// <summary>
        /// le nombre de liste a chaque refresh.
        /// </summary>
        private int _listViewCount = 0;

        /// <summary>
        /// le nombre de panel scrollables a chaque refresh.
        /// </summary>
        private int _scrollPanCount = 0;

        /// <summary>
        /// le nombre de panel Preview animation a chaque refresh.
        /// </summary>
        private int _animPreviewCount = 0;

        /// <summary>
        /// empeche de set les styles plusieur fois.
        /// </summary>
        private bool _multipleStyleSetLock;

        /// <summary>
        /// Les panels crees accompagne de leur vector de position de scroll
        /// </summary>
        private Dictionary<int, Vector2> _panelsScrools = new Dictionary<int, Vector2>();

        /// <summary>
        /// Les Listes crees accompagne de leur vector de position de scroll
        /// </summary>
        private Dictionary<int, Vector2> _listsScrolls = new Dictionary<int, Vector2>();

        /// <summary>
        /// Les previews crees accompagne de leur objects de preview
        /// </summary>
        private Dictionary<int, AnimationPreviewer> _animsPreviews = new Dictionary<int, AnimationPreviewer>();

        #endregion

        #region Proprietes ##########################################################################

        /// <summary>
        /// L'emplacement generique des assets
        /// </summary>
        protected abstract string Save_Path { get; }

        /// <summary>
        /// Le nom generique des assets
        /// </summary>
        protected abstract string SaveFileName { get; }

        /// <summary>
        /// La taille par defaut des fenetre de l'editeur.
        /// </summary>
        protected Vector2 DefaultWindowSize { get { return new Vector2(500, 900); } }

        #endregion

        #region Styles ##########################################################################

        /// <summary>
        /// le style des items d'une liste.
        /// </summary>
        protected GUIStyle style_listItem;

        /// <summary>
        /// le style des editeurs multi ligne.
        /// </summary>
        protected GUIStyle style_txtArea;

        /// <summary>
        /// le style des groupes.
        /// </summary>
        protected GUIStyle style_group;

        /// <summary>
        /// le style des grilles de nodes.
        /// </summary>
        protected GUIStyle style_grid;

        /// <summary>
        /// le style des Nodes.
        /// </summary>
        protected GUIStyle style_node;

        /// <summary>
        /// le style des Nodes speciaux.
        /// </summary>
        protected GUIStyle style_nodeSpecials;

        /// <summary>
        /// le style des labels.
        /// </summary>
        protected GUIStyle style_label;

        /// <summary>
        /// le style des selecteur d'objet.
        /// </summary>
        protected GUILayoutOption[] style_objSelect;

        #endregion

        #region Events ######################################################################

        /// <summary>
        /// A different saving manner, for modules that require it.
        /// </summary>
        protected Action customSave;

        /// <summary>
        /// Action on selection, for modules that require custom selection method.
        /// </summary>
        protected Action SelectAction;

        #endregion

        #region Signals ##########################################################################


        /// <summary>
        /// Appellee au demarrage de la fenetre, a utiliser a la place de OnEnable dans les fenetres heritantes
        /// </summary>
        protected virtual void OnInitialize()
        {

        }

        /// <summary>
        /// Appellee lorsqu'on ferme la fenetre.
        /// </summary>
        protected virtual void OnQuit()
        {

        }

        /// <summary>
        /// Appellee a chaque rafraichissement de la fenetre, a utiliser a la place de onGUI dans les fenetres heritantes
        /// </summary>
        protected virtual void OnRedraw()
        {
            GUILayout.BeginVertical();
            OnHeaderRedraw();
            GUILayout.BeginHorizontal();
            //left panel
            try
            {
                if (currentEditorMode != EditorMode.DataEdition && dataList != null)
                {
                    string[] names = new string[dataList.Count];
                    if (dataList.Count > 0)
                    {
                        //TODO: Manage Traduction of displayed names
                        for (int i = 0, len = dataList.Count; i < len; i++)
                        {
                            names[i] = dataList[i].Id + " -> " + dataList[i].Name;
                        }
                    }
                    //filtering here
                    //TODO: function to filter the list here
                    //sorting here
                    //TODO: function to sort the list here
                    //displaying
                    ScrollablePanel(() =>
                    {
                    //search and filter
                    //listing
                    GroupGUI(() =>
                        {
                            MakeList(selectDataIndex, names, index => selectDataIndex = index, dataList.ToArray());
                        //if (selectDataIndex >= 0 && selectDataIndex < dataList.Count)
                        //    data = dataList[selectDataIndex];
                        OnListButtons();
                        }, "Items List");
                    //foot panel
                    OnFootRedraw();
                    //save panel
                    SaveBarPanel();

                    }, true);
                }
            }
            catch (Exception e)
            {
                PulseDebug.LogError("Exeption thrown " + e.Message);
                CloseWindow();
            }
            ScrollablePanel(() =>
            {
            //right panel
            OnBodyRedraw();
            });
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ////save panel
            //SaveBarPanel();
            ////foot panel
            //OnFootRedraw();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Appellee a chaque rafraichissement de la fenetre, a l'entete.
        /// </summary>
        protected virtual void OnHeaderRedraw()
        {

        }

        /// <summary>
        /// Appellee a chaque rafraichissement de la fenetre, au corps de page.
        /// </summary>
        protected virtual void OnBodyRedraw()
        {

        }

        /// <summary>
        /// Appellee a chaque rafraichissement de la fenetre, au pied de page.
        /// </summary>
        protected virtual void OnFootRedraw()
        {

        }

        /// <summary>
        /// Appellee a chaque rafraichissement de la fenetre, pour afficher les bouttons de pied de liste.
        /// </summary>
        protected virtual void OnListButtons()
        {
            StandartListButtons();
        }

        /// <summary>
        /// Au changement d'une selection dans une Entete.
        /// </summary>
        protected virtual void OnHeaderChange()
        {

        }

        /// <summary>
        /// Au changement d'une selection dans une liste ou grille.
        /// </summary>
        protected virtual void OnListChange()
        {

        }

        /// <summary>
        /// Apelle lorsque l'on supprime un asset
        /// </summary>
        /// <param name="asset"></param>
        protected virtual void OnRemoveAssetDelete(T asset) { }

        #endregion

        #region GuiMethods ##########################################################################

        /// <summary>
        /// Pour initialiser les styles.
        /// </summary>
        private void StyleSetter()
        {
            if (_multipleStyleSetLock)
                return;
            //Text Area
            style_txtArea = new GUIStyle(GUI.skin.textArea);
            //list field
            style_listItem = new GUIStyle(GUI.skin.textField);
            style_listItem.onNormal.textColor = Color.blue;
            style_listItem.onHover.textColor = Color.blue;
            style_listItem.onActive.textColor = Color.blue;
            style_listItem.hover.textColor = Color.black;
            style_listItem.normal.textColor = Color.gray;
            style_listItem.clipping = TextClipping.Clip;
            //groupes
            style_group = new GUIStyle(GUI.skin.window);
            style_group.stretchWidth = false;
            style_group.fontStyle = FontStyle.Bold;
            style_group.margin = new RectOffset(8, 8, 5, 8);
            //labels
            style_label = new GUIStyle(GUI.skin.label);
            style_label.stretchWidth = false;
            style_label.fixedWidth = 120;
            style_label.fontStyle = FontStyle.Bold;
            //obj select
            style_objSelect = new[] { GUILayout.Width(150), GUILayout.Height(150) };
            //Nodes
            style_node = new GUIStyle("Button");
            style_node.focused.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            style_node.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node0.png") as Texture2D;
            style_node.normal.textColor = Color.white;
            style_node.hover.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node2.png") as Texture2D;
            style_node.hover.textColor = Color.white;
            style_node.alignment = TextAnchor.MiddleCenter;
            style_node.fontStyle = FontStyle.Bold;
            {
                int border = 15;
                style_node.border = new RectOffset(border, border, border, border);
            }
            //Nodes special
            style_nodeSpecials = new GUIStyle("Button");
            style_nodeSpecials.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            style_nodeSpecials.normal.textColor = Color.white;
            style_nodeSpecials.alignment = TextAnchor.MiddleCenter;
            {
                int border = 15;
                style_nodeSpecials.border = new RectOffset(border, border, border, border);
            }
            //Grid
            style_grid = new GUIStyle(GUI.skin.window);
            Vector2Int scale = new Vector2Int(1024, 1024 * (16 / 9));
            var gridTexture = new Texture2D(scale.x, scale.y);
            float caseRatio = 0.95f;
            float linesTickness = (1 - caseRatio) / 2;
            float aspectratio = gridTexture.width / gridTexture.height;
            int caseNumX = 64;
            int caseNumY = Mathf.RoundToInt(caseNumX * aspectratio);
            int caseSizeX = gridTexture.width / caseNumX;
            int caseSizeY = gridTexture.height / caseNumY;
            for (int i = 0; i < gridTexture.width; i++)
            {
                float progressionX = i % caseSizeX;
                float xpercent = (progressionX / caseSizeX);
                bool inMiddleX = xpercent > linesTickness && xpercent <= (caseRatio + linesTickness);
                for (int j = 0; j < gridTexture.height; j++)
                {
                    float progressionY = j % caseSizeY;
                    float ypercent = (progressionY / caseSizeY);
                    bool inMiddleY = ypercent > linesTickness && ypercent <= (caseRatio + linesTickness);
                    Color c = inMiddleX && inMiddleY ? new Color(0, 0, 0, 0.15f) : new Color(1, 1, 1, 0.5f);
                    gridTexture.SetPixel(i, j, c);
                }
            }
            gridTexture.Apply();
            style_grid.normal.background = gridTexture;

            _multipleStyleSetLock = true;
        }

        /// <summary>
        /// Un champs texte a caracteres limites.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        protected string LimitText(string input)
        {
            string str = EditorGUILayout.TextArea(input, style_txtArea);
            char[] cutted = new char[FIELD_MAX_CHARACTERS];
            for (int i = 0; i < FIELD_MAX_CHARACTERS; i++)
                if (str.Length > i)
                    cutted[i] = str[i];
            string ret = new string(cutted);
            return ret;
        }

        /// <summary>
        /// Faire un group d'items
        /// </summary>
        /// <param name="guiFunctions"></param>
        /// <param name="groupTitle"></param>
        protected void GroupGUI(Action guiFunctions, int widht)
        {
            GUILayout.BeginVertical("", style_group, new[] { GUILayout.Width(widht) });
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("GroupBox");
            if (guiFunctions != null)
                guiFunctions.Invoke();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Faire un group d'items sans le style d'interieur
        /// </summary>
        /// <param name="guiFunctions"></param>
        /// <param name="groupTitle"></param>
        protected void GroupGUInoStyle(Action guiFunctions, int width)
        {
            GUILayout.BeginVertical("", style_group, new[] { GUILayout.Width(width) });
            if (guiFunctions != null)
                guiFunctions.Invoke();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Faire un group d'items
        /// </summary>
        /// <param name="guiFunctions"></param>
        /// <param name="groupTitle"></param>
        protected void GroupGUI(Action guiFunctions, string groupTitle, Vector2 size)
        {
            List<GUILayoutOption> options = new List<GUILayoutOption>();
            if (size.y > 0)
                options.Add(GUILayout.Height(size.y));
            if (size.x > 0)
                options.Add(GUILayout.Width(size.x));
            GUILayout.BeginVertical(groupTitle, style_group, options.ToArray());
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("GroupBox");
            if (guiFunctions != null)
                guiFunctions.Invoke();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Faire un group d'items
        /// </summary>
        /// <param name="guiFunctions"></param>
        /// <param name="groupTitle"></param>
        protected void GroupGUI(Action guiFunctions, string groupTitle = "", int height = 0)
        {
            List<GUILayoutOption> options = new List<GUILayoutOption>();
            if (height > 0)
                options.Add(GUILayout.Height(height));
            GUILayout.BeginVertical(groupTitle, style_group, options.ToArray());
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("GroupBox");
            if (guiFunctions != null)
                guiFunctions.Invoke();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Faire un group d'items sans le style d'interieur
        /// </summary>
        /// <param name="guiFunctions"></param>
        /// <param name="groupTitle"></param>
        protected void GroupGUInoStyle(Action guiFunctions, string groupTitle = "", int height = 0)
        {
            List<GUILayoutOption> options = new List<GUILayoutOption>();
            if (height > 0)
                options.Add(GUILayout.Height(height));
            GUILayout.BeginVertical(groupTitle, style_group, options.ToArray());
            if (guiFunctions != null)
                guiFunctions.Invoke();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Panel, generalement en bas de fenetre conteneant le plus souvent les bouttons 'save' et 'cancel'
        /// </summary>
        /// <param name="actionButtons"></param>
        protected void SaveCancelPanel(params KeyValuePair<string, Action>[] actionButtons)
        {
            GroupGUInoStyle(() =>
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < actionButtons.Length; i++)
                {
                    if (GUILayout.Button(actionButtons[i].Key)) { if (actionButtons[i].Value != null) actionButtons[i].Value.Invoke(); }
                }
                GUILayout.EndHorizontal();
            }, "", 50);
        }

        /// <summary>
        /// Un panel classique de sauvegarde.
        /// </summary>
        /// <param name="_toSave"></param>
        /// <param name="_whereSave"></param>
        protected void SaveBarPanel()
        {
            switch (currentEditorMode)
            {
                case EditorMode.Edition:
                    SaveCancelPanel(new[] {
                        new KeyValuePair<string, Action>("Save", ()=> {
                            if (customSave != null)
                                customSave.Invoke();
                            else
                                SaveAsset();
                        }),
                        new KeyValuePair<string, Action>("Close", ()=> {
                            if (EditorUtility.DisplayDialog("Warning", "The Changes you made won't be saved.\n Proceed?","Yes","No"))
                                Close();
                        })
                    });
                    break;
                case EditorMode.Selection:
                    SaveCancelPanel(new[] {
                        new KeyValuePair<string, Action>("Select", ()=> {
                            if (customSave != null)
                                customSave.Invoke();
                            else
                                SaveAsset();
                            if (SelectAction != null)
                                SelectAction.Invoke();
                            else
                            {
                                //if(onSelectionEvent != null)
                                //    onSelectionEvent.Invoke(data, new EditorEventArgs{ dataObjectLocation = ((IData)data).Location });
                            }
                            Close();
                        }),
                        new KeyValuePair<string, Action>("Cancel", ()=> { if(EditorUtility.DisplayDialog("Warning", "The Selection you made won't be saved.\n Proceed?","Yes","No")) Close();})
                    });
                    break;
                case EditorMode.DataEdition:
                    SaveCancelPanel(new[] {
                        new KeyValuePair<string, Action>("Save", ()=> {
                            if (customSave != null)
                                customSave.Invoke();
                            else
                                SaveAsset();
                            Close();
                        }),
                        new KeyValuePair<string, Action>("Close", ()=> { if(EditorUtility.DisplayDialog("Warning", "The Changes you made won't be saved.\n Proceed?","Yes","No")) Close();})
                    });
                    break;
                case EditorMode.Preview:
                    break;
                case EditorMode.NodeGraph:
                    break;
            }
        }

        /// <summary>
        /// Faire une liste d'elements, et renvoyer l'element selectionne.
        /// </summary>
        /// <param name="listID"></param>
        /// <param name="content"></param>
        protected int ListItems(int selected = -1, params GUIContent[] content)
        {
            _listViewCount++;
            Vector2 scroolPos = Vector2.zero;
            if (_listsScrolls == null)
                _listsScrolls = new Dictionary<int, Vector2>();
            if (_listsScrolls.ContainsKey(_listViewCount))
                scroolPos = _listsScrolls[_listViewCount];
            else
                _listsScrolls.Add(_listViewCount, scroolPos);
            scroolPos = GUILayout.BeginScrollView(scroolPos);
            GUILayout.BeginVertical();
            int sel = GUILayout.SelectionGrid(selected, content, 1, style_listItem);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            _listsScrolls[_listViewCount] = scroolPos;
            return sel;
        }

        /// <summary>
        /// Faire une Grille d'elements, et renvoyer l'element selectionne.
        /// </summary>
        /// <param name="listID"></param>
        /// <param name="content"></param>
        protected int GridItems(int selected = -1, int xSize = 2, params GUIContent[] content)
        {
            _listViewCount++;
            Vector2 scroolPos = Vector2.zero;
            if (_listsScrolls.ContainsKey(_listViewCount))
                scroolPos = _listsScrolls[_listViewCount];
            else
                _listsScrolls.Add(_listViewCount, scroolPos);
            scroolPos = GUILayout.BeginScrollView(scroolPos);
            GUILayout.BeginVertical();
            int sel = GUILayout.SelectionGrid(selected, content, xSize);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            _listsScrolls[_listViewCount] = scroolPos;
            return sel;
        }

        /// <summary>
        /// Faire une liste d'elements, et renvoyer l'element selectionne en envoyant un signal a la modification.
        /// </summary>
        /// <param name="_index"></param>
        /// <param name="_collection"></param>
        /// <returns></returns>
        protected int MakeList(int _index, string[] _collection, Action<int> beforeChange = null, T[] dataCollection = null)
        {
            List<GUIContent> listContent = new List<GUIContent>();
            for (int i = 0; i < _collection.Length; i++)
            {
                var name = _collection[i];
                char[] titleChars = new char[LIST_MAX_CHARACTERS];
                string pointDeSuspension = string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    for (int j = 0; j < titleChars.Length; j++)
                        if (j < name.Length)
                            titleChars[j] = name[j];
                }
                if (name.Length >= titleChars.Length)
                    pointDeSuspension = "...";
                string title = string.IsNullOrEmpty(name) ? "<<<< None >>>>" : new string(titleChars) + pointDeSuspension;
                listContent.Add(new GUIContent { text = (i + 1) + " | " + title });
            }
            int tmp = ListItems(_index, listContent.ToArray());
            if (beforeChange != null)
                beforeChange.Invoke(tmp);
            if (tmp != _index)
                ListChange();
            if (dataCollection != null && dataCollection.Length > 0)
            {
                if (tmp >= 0 && tmp < dataCollection.Length)
                    data = dataCollection[tmp];
                else
                    data = null;
            }
            return tmp;
        }

        /// <summary>
        /// Faire une grille de 4 collones d'elements, et renvoyer l'element selectionne en envoyant un signal a la modification.
        /// </summary>
        /// <param name="_index"></param>
        /// <param name="_collection"></param>
        /// <returns></returns>
        protected int MakeGrid(int _index, string[] _collection, params T[] dataCollection)
        {
            List<GUIContent> listContent = new List<GUIContent>();
            for (int i = 0; i < _collection.Length; i++)
            {
                var name = _collection[i];
                char[] titleChars = new char[LIST_MAX_CHARACTERS];
                string pointDeSuspension = string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    for (int j = 0; j < titleChars.Length; j++)
                        if (j < name.Length)
                            titleChars[j] = name[j];
                }
                if (name.Length >= titleChars.Length)
                    pointDeSuspension = "...";
                string title = string.IsNullOrEmpty(name) ? "<<<< None >>>>" : new string(titleChars) + pointDeSuspension;
                listContent.Add(new GUIContent { text = i + "-" + title });
            }
            int tmp = GridItems(_index, 4, listContent.ToArray());
            if (tmp != _index)
                ListChange();
            if (dataCollection != null && dataCollection.Length > 0)
            {
                if (tmp >= 0 && tmp < dataCollection.Length)
                    data = dataCollection[tmp];
                else
                    data = null;
            }
            return tmp;
        }

        /// <summary>
        /// Faire une entete d'elements, et renvoyer l'element selectionne en envoyant un signal a la modification.
        /// </summary>
        /// <param name="_index"></param>
        /// <param name="_collection"></param>
        /// <returns></returns>
        protected int MakeHeader(int _index, string[] _collection, Action<int> beforeEmitSignal = null)
        {
            int selId = GUILayout.Toolbar(_index, _collection);
            if (beforeEmitSignal != null)
                beforeEmitSignal.Invoke(selId);
            if (selId != _index || data == null)
                HeaderChange();
            return selId;
        }

        /// <summary>
        /// faire un panel scroolable verticalement.
        /// </summary>
        /// <param name="guiFunctions"></param>
        /// <param name="groupTitle"></param>
        protected void ScrollablePanel(Action guiFunctions = null, bool fixedSize = false)
        {
            _scrollPanCount++;
            Vector2 scroolPos = Vector2.zero;
            if (_panelsScrools == null)
                _panelsScrools = new Dictionary<int, Vector2>();
            if (_panelsScrools.ContainsKey(_scrollPanCount))
                scroolPos = _panelsScrools[_scrollPanCount];
            else
                _panelsScrools.Add(_scrollPanCount, scroolPos);
            var options = new[] { /*GUILayout.MinWidth(100),*/ GUILayout.Width(minSize.x / 2) };
            scroolPos = GUILayout.BeginScrollView(scroolPos, fixedSize ? options : null);
            GUILayout.BeginVertical();
            if (guiFunctions != null)
                guiFunctions.Invoke();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            _panelsScrools[_scrollPanCount] = scroolPos;
        }

        /// <summary>
        /// The standart Add, Remove, Copy/Paste buttons on list of Datas.
        /// </summary>
        protected void StandartListButtons()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                int maxId = dataList != null ? dataList.Count : 0;
                var item = ScriptableObject.CreateInstance<T>();
                if (item != null)
                {
                    item.WriteField<ScriptableResource>("_id", maxId);
                    string path = Path.Combine(PulseConstants.GAME_RES_PATH + Save_Path, SaveFileName + "_" + maxId + ".asset");
                    if (delAssetPath.Contains(path))
                    {
                        delAssetPath.Remove(path);
                    }
                    dataList.Add(item);
                }
            }
            if (dataList == null || dataList.Count <= 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }
            if (data != null)
            {
                if (GUILayout.Button("Remove"))
                {
                    int index = dataList.FindIndex(item => { return ReferenceEquals(item, data); });
                    if (index >= 0)
                    {
                        string path = AssetDatabase.GetAssetPath(dataList[index]);
                        if (!string.IsNullOrEmpty(path))
                        {
                            delAssetPath.Add(path);
                        }
                        dataList.RemoveAt(index);
                    }
                    ListChange();
                }
                //if (clipBoard != null)
                //{
                //    if (GUILayout.Button("Paste"))
                //    {
                //        int maxID = 0;
                //        for (int i = 0; i < dataList.Count; i++)
                //        {
                //            var iiTem = dataList[i] as IData;
                //            if (iiTem == null)
                //                continue;
                //            if (iiTem.Location.id > maxID)
                //                maxID = iiTem.Location.id;
                //        }
                //        var iClipBoard = clipBoard as IData;
                //        if (iClipBoard != null)
                //        {
                //            DataLocation loc = iClipBoard.Location;
                //            loc.id = maxID + 1;
                //            iClipBoard.Location = loc;
                //            int nextID = selectDataIndex + 1;
                //            if (nextID >= 0 && nextID < dataList.Count)
                //            {
                //                dataList.Insert(nextID, iClipBoard);
                //            }
                //            else
                //                dataList.Add(iClipBoard);
                //            ListChange();
                //        }
                //    }
                //    if (GUILayout.Button("X"))
                //    {
                //        clipBoard = null;
                //    }
                //}
                //else
                //{
                //    if (GUILayout.Button("Copy"))
                //    {
                //        clipBoard = Core.ObjectClone(data);
                //    }
                //}
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Previsualize une animation
        /// </summary>
        protected void PreviewAnAnimation(Motion _motion, float aspectRatio = 1.77f, GameObject _target = null, params (GameObject go, HumanBodyBones bone, Vector3 offset, Vector3 rotation, Vector3 scale)[] accessories)
        {
            _animPreviewCount++;
            AnimationPreviewer preview = null;
            if (_animsPreviews == null)
                _animsPreviews = new Dictionary<int, AnimationPreviewer>();
            if (_animsPreviews.ContainsKey(_animPreviewCount))
                preview = _animsPreviews[_animPreviewCount];
            else
                _animsPreviews.Add(_animPreviewCount, new AnimationPreviewer());
            if (!preview && _animsPreviews.ContainsKey(_animPreviewCount))
                preview = _animsPreviews[_animPreviewCount];
            if (!preview)
                return;
            preview.Previsualize(_motion, aspectRatio, _target, accessories);
            _animsPreviews[_animPreviewCount] = preview;
        }

        /// <summary>
        /// Reset the previews
        /// </summary>
        protected void ResetPreviews(bool destroy = false)
        {
            if (_animsPreviews == null)
                return;
            //for (int len = AnimsPreviews.Count, i = len; i >= 0; i--)
            //{
            //    var preview = AnimsPreviews.ElementAt(i);
            //    var key = preview.Key;
            //    AnimsPreviews[key].Destroy();
            //}
            //if (destroy)
            _animsPreviews.Clear();
        }

        #endregion

        #region Mono #######################################################################

        private void Update()
        {
            Repaint();
        }

        private void OnEnable()
        {
            minSize = new Vector2(600, 600);
            Focus();
            OnInitialize();
        }

        private void OnGUI()
        {
            StyleSetter();
            _listViewCount = 0;
            _scrollPanCount = 0;
            _animPreviewCount = 0;
            OnRedraw();
        }

        private void OnDisable()
        {
            clipBoard = null;
            ResetPreviews(true);
            //if (currentEditorMode == EditorMode.DataEdition)
            //{
            //    SaveAsset(asset, originalAsset);
            //}
            //RefreshCache(editorDataType);
            OnQuit();
            //OnCacheRefresh = delegate { };
            //CloseWindow();
        }

        #endregion

        #region Methods #############################################################################

        /// <summary>
        /// Pour sauvegarder des changements effectues sur un asset clone.
        /// </summary>
        /// <param name="edited"></param>
        /// <param name="loaded"></param>
        protected void SaveAsset()
        {
            if (!string.IsNullOrEmpty(SaveFileName))
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    if (dataList[i] == null)
                        continue;
                    string path = Path.Combine(PulseConstants.GAME_RES_PATH + Save_Path, SaveFileName + "_" + dataList[i].Id + ".asset");
                    if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
                    {
                        AssetDatabase.CreateAsset(dataList[i], path);
                    }
                    else
                    {
                        EditorUtility.SetDirty(dataList[i]);
                    }
                }
            }
            else
            {
                PulseDebug.LogError("Invalid File Name");
            }
            for (int i = 0; i < delAssetPath.Count; i++)
            {
                if (string.IsNullOrEmpty(delAssetPath[i]))
                    continue;
                T delAsset = AssetDatabase.LoadAssetAtPath<T>(delAssetPath[i]);
                if (delAsset != null)
                {
                    OnRemoveAssetDelete(delAsset);
                    AssetDatabase.DeleteAsset(delAssetPath[i]);
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// ferme la fenetre
        /// </summary>
        private void CloseWindow()
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        /// <summary>
        /// Au changement d'une selection dans une Entete.
        /// </summary>
        private void HeaderChange()
        {
            if (data != null)
            {
                SaveAsset();
            }
            originalAsset = null;
            data = null;
            selectDataIndex = -1;
            dataList.Clear();
            clipBoard = null;
            GUI.FocusControl(null);
            OnInitialize();
            OnHeaderChange();
        }

        /// <summary>
        /// Au changement d'une selection dans une liste ou grille.
        /// </summary>
        private void ListChange()
        {
            //if (selectDataIndex >= 0 && data != null)
            //{
            //    var location_Prop = dataList[0].GetType().GetProperty("Location");
            //    if (location_Prop != null)
            //    {
            //        DataLocation locValue = (DataLocation)location_Prop.GetValue(data);
            //        if (locValue != default(DataLocation))
            //        {
            //            int correspondingIndex = dataList.FindIndex(dt =>
            //            {
            //                DataLocation objLoc = (DataLocation)location_Prop.GetValue(dt);
            //                return locValue == objLoc;
            //            });
            //            selectDataIndex = correspondingIndex;
            //        }
            //    }
            //}
            data = null;
            dataID = -1;
            GUI.FocusControl(null);
            //OnInitialize();
            OnListChange();
        }

        #endregion

        #region utils #########################################################################

        /// <summary>
        /// Return a texture colored.
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Texture2D ColorToTexture(Color col)
        {
            return default;
        }

        #endregion
    }

}