﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CognitiveVR
{
    [InitializeOnLoad]
    public class CognitiveVR_Settings : EditorWindow
    {
        public static Color GreenButton = new Color(0.4f, 1f, 0.4f);


        public static Color OrangeButton = new Color(1f, 0.6f, 0.3f);
        public static Color OrangeButtonPro = new Color(1f, 0.8f, 0.5f);

        public static CognitiveVR_Settings Instance;

        /// <summary>
        /// used for 'step' box text
        /// </summary>
        public static string GreyTextColorString
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return "<color=#aaaaaaff>";
                return "<color=#666666ff>";
            }
        }

        static string SdkVersionUrl = "https://s3.amazonaws.com/cvr-test/sdkversion.txt";

        System.DateTime lastSdkUpdateDate; // when cvr_version was last set
        //cvr_skipVersion - EditorPref if newVersion == skipVersion, don't show update window

        [MenuItem("cognitiveVR/Settings Window", priority = 1)]
        public static void Init()
        {
            EditorApplication.update -= EditorUpdate;

            // Get existing open window or if none, make a new one:
            Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings");
            Vector2 size = new Vector2(300, 550);
            Instance.minSize = size;
            Instance.maxSize = size;
            Instance.Show();

            //fix this. shouldn't have to re-read this value to parse it to the correct format
            if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out Instance.lastSdkUpdateDate))
            {
                //Instance.lastSdkUpdateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                Debug.Log("CognitiveVR_Settings failed to parse UpdateDate");
            }
        }

        static CognitiveVR_Settings()
        {
            EditorApplication.update += EditorUpdate;
        }

        static void EditorUpdate()
        {


            bool show = true;

#if CVR_STEAMVR || CVR_OCULUS || CVR_GOOGLEVR || CVR_DEFAULT || CVR_FOVE || CVR_PUPIL
            show = false;
#endif

            string version = EditorPrefs.GetString("cvr_version");
            if (string.IsNullOrEmpty(version) || version != CognitiveVR.Core.SDK_Version)
            {
                show = true;
                //new version installed
            }

            if (show)
            {
                Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings");
                Vector2 size = new Vector2(300, 550);
                Instance.minSize = size;
                Instance.maxSize = size;
                Instance.Show();


                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out Instance.lastSdkUpdateDate))
                {
                    //Instance.updateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            System.DateTime remindDate; //current date must be this or beyond to show popup window

            if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), out remindDate))
            {
                if (System.DateTime.UtcNow > remindDate)
                {
                    CheckForUpdates();
                }
            }
            else
            {
                Debug.Log("CognitiveVR_Settings Failed to parse cvr_updateRemindDate " + EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"));
                CheckForUpdates();
            }

            EditorApplication.update -= EditorUpdate;
        }

        string GetSamplesResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = System.IO.Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "CognitiveVR/Editor".Length) + "";
        }

        string GetResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = System.IO.Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Editor".Length) + "";
        }

        static int productIndex = 0; //used in dropdown menu
        static int organizationIndex = 0; //used in dropdown menu
        string password;
        Rect productRect;
        Rect sdkRect;

        int testprodSelection = -1;

        public void OnGUI()
        {
            GUI.skin.label.richText = true;
            GUI.skin.button.richText = true;
            GUI.skin.box.richText = true;

            CognitiveVR.CognitiveVR_Preferences prefs = GetPreferences();

            if (prefs.SelectedOrganization == null) { prefs.SelectedOrganization = new Json.Organization(); }
            if (prefs.SelectedProduct == null) { prefs.SelectedProduct = new Json.Product(); }

            if (testprodSelection == -1)
            {
                if (prefs.CustomerID.EndsWith("-prod"))
                    testprodSelection = 1;
                if (prefs.CustomerID.EndsWith("-test"))
                    testprodSelection = 0;
            }

            //LOGO
            var resourcePath = GetResourcePath();
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(resourcePath + "Textures/logo-light.png");

            var rect = GUILayoutUtility.GetRect(0, 0, 0, 100);

            if (logo)
            {
                rect.width -= 20;
                rect.x += 10;
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);
            }

            //=========================
            //Authenticate
            //=========================



            GUILayout.BeginHorizontal();

            UserStartupBox("1", IsUserLoggedIn());


            GUILayout.FlexibleSpace();

            GUILayout.Label("<size=14><b>Authenticate</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(IsUserLoggedIn());

            prefs.UserName = GhostTextField("name@email.com", "", prefs.UserName);

            if (Event.current.character == '\n' && Event.current.type == EventType.KeyDown)
            {
                RequestLogin(prefs.UserName, password);
            }

            password = GhostPasswordField("password", "", password);



            EditorGUI.EndDisabledGroup();

            if (IsUserLoggedIn())
            {
                if (GUILayout.Button("Logout"))
                {
                    Logout();
                }
            }
            else
            {
                if (GUILayout.Button("Login"))
                {
                    RequestLogin(prefs.UserName, password);
                }
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (EditorGUIUtility.isProSkin)
                {
                    GUIStyle s = new GUIStyle(EditorStyles.whiteLabel);
                    s.normal.textColor = new Color(0.6f, 0.6f, 1);
                    if (GUILayout.Button("Create Account", s))
                        Application.OpenURL("https://dashboard.cognitivevr.io/#/login");
                }
                else
                {
                    GUIStyle s = new GUIStyle(EditorStyles.whiteLabel);
                    s.normal.textColor = Color.blue;
                    if (GUILayout.Button("Create Account", s))
                        Application.OpenURL("https://dashboard.cognitivevr.io/#/login");
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);


            //=========================
            //Select organization
            //=========================
            if (IsUserLoggedIn())
            {
                EditorGUI.BeginDisabledGroup(!IsUserLoggedIn());

                EditorGUILayout.BeginHorizontal();

                UserStartupBox("2", !string.IsNullOrEmpty(prefs.CustomerID) || (prefs.SelectedOrganization != null && !string.IsNullOrEmpty(prefs.SelectedOrganization.name)));

                GUILayout.FlexibleSpace();
                GUILayout.Label("Select Organization", CognitiveVR_Settings.HeaderStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                string lastOrganization = prefs.SelectedOrganization.name; //used to check if organizations changed. for displaying products
                if (!string.IsNullOrEmpty(prefs.SelectedOrganization.name))
                {
                    for (int i = 0; i < prefs.UserData.organizations.Length; i++)
                    {
                        if (prefs.UserData.organizations[i].name == prefs.SelectedOrganization.name)
                        {
                            organizationIndex = i;
                            break;
                        }
                    }
                }

                string[] organizations = GetUserOrganizations();
                if (organizations.Length <= 0 && IsUserLoggedIn())
                    GUILayout.Label("No Organizations Exist!", new GUIStyle(EditorStyles.popup));
                else
                    organizationIndex = EditorGUILayout.Popup(organizationIndex, organizations);


                if (organizations.Length > 0)
                {
                    //TODO fix this some other day
                    try
                    {
                        prefs.SelectedOrganization = GetPreferences().GetOrganization(organizations[organizationIndex]);
                    }
                    catch
                    {
                        organizationIndex = 0;
                        prefs.SelectedOrganization = GetPreferences().GetOrganization(organizations[0]);
                    }
                }

                //=========================
                //select product
                //=========================

                EditorGUILayout.BeginHorizontal();

                UserStartupBox("3", !string.IsNullOrEmpty(prefs.CustomerID) || (prefs.SelectedProduct != null && !string.IsNullOrEmpty(prefs.SelectedProduct.name)));

                GUILayout.FlexibleSpace();
                GUILayout.Label("Select Product", CognitiveVR_Settings.HeaderStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();



                string organizationName = "";
                if (organizations.Length > 0)
                    organizationName = organizations[organizationIndex];

                string[] products = GetVRProductNames(organizationName);

                if (lastOrganization != prefs.SelectedOrganization.name)
                {
                    //ie, changed organization
                    prefs.SelectedProduct = null;
                    productIndex = 0;
                }
                else
                {
                    if (prefs.SelectedProduct != null && !string.IsNullOrEmpty(prefs.SelectedProduct.name))
                    {
                        for (int i = 0; i < products.Length; i++)
                        {
                            if (products[i] == prefs.SelectedProduct.name)
                            {
                                productIndex = i;
                                break;
                            }
                        }
                    }
                }

                GUILayout.BeginHorizontal();

                if (products.Length <= 0 && IsUserLoggedIn())
                    GUILayout.Label("No Products Exist!", new GUIStyle(EditorStyles.popup));
                else
                    productIndex = EditorGUILayout.Popup(productIndex, products);
                //productIndex = EditorGUILayout.Popup(productIndex, products);

                if (products.Length > 0)
                    prefs.SelectedProduct = GetPreferences().GetProduct(products[productIndex]);

                if (GUILayout.Button("New", GUILayout.Width(40)))
                {
                    productRect.y -= 20;
                    productRect.x += 50;
                    PopupWindow.Show(productRect, new CognitiveVR_NewProductPopup());
                }
                if (Event.current.type == EventType.Repaint) productRect = GUILayoutUtility.GetLastRect();

                GUILayout.EndHorizontal();


                //=========================
                //test prod
                //=========================
                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(products.Length <= 0);
                GUIStyle testStyle = new GUIStyle(EditorStyles.miniButtonLeft);
                GUIStyle prodStyle = new GUIStyle(EditorStyles.miniButtonRight);
                if (testprodSelection == 0)
                {
                    testStyle.normal = testStyle.active;
                }
                else if (testprodSelection == 1)
                {
                    prodStyle.normal = prodStyle.active;
                }

                if (GUILayout.Button(new GUIContent("Test", "Send data to your internal development server"), testStyle))
                {
                    testprodSelection = 0;
                    SaveSettings();
                }

                if (GUILayout.Button(new GUIContent("Prod", "Send data to your server for players"), prodStyle))
                {
                    testprodSelection = 1;
                    SaveSettings();
                }
                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUILayout.Space(89);
            }

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            if ((prefs.SelectedProduct != null && !string.IsNullOrEmpty(prefs.SelectedProduct.name) && !string.IsNullOrEmpty(prefs.SelectedOrganization.name) && !string.IsNullOrEmpty(prefs.CustomerID)))
            {
                EditorGUI.BeginDisabledGroup(prefs.SelectedProduct == null || string.IsNullOrEmpty(prefs.SelectedProduct.name) || string.IsNullOrEmpty(prefs.SelectedOrganization.name));

                //=========================
                //select vr sdk
                //=========================
                GUILayout.BeginHorizontal();


#if CVR_STEAMVR || CVR_OCULUS || CVR_GOOGLEVR || CVR_DEFAULT || CVR_FOVE || CVR_PUPIL
                UserStartupBox("4", true);
#else
                UserStartupBox("4", false);
#endif

                if (GUILayout.Button("Select SDK"))
                {
                    sdkRect.x += 280;
                    sdkRect.y -= 20;

                    PopupWindow.Show(sdkRect, new CognitiveVR_SelectSDKPopup());
                }
                if (Event.current.type == EventType.Repaint) sdkRect = GUILayoutUtility.GetLastRect();
                GUILayout.EndHorizontal();

                //=========================
                //options
                //=========================

                GUILayout.BeginHorizontal();
                CognitiveVR_Manager manager = FindObjectOfType<CognitiveVR_Manager>();

                UserStartupBox("5", manager != null);

                if (GUILayout.Button("Track Player Actions"))
                {
                    CognitiveVR_ComponentSetup.Init();
                }

                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();

                var scenedata = CognitiveVR_Preferences.Instance.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);

                UserStartupBox("6", scenedata != null && scenedata.LastRevision > 0);

                if (GUILayout.Button("Upload Scene"))
                {
                    CognitiveVR_SceneExportWindow.Init();
                }
                GUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUILayout.Space(67);
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            //updates
            if (GUILayout.Button("Check for Updates"))
            {
                EditorPrefs.SetString("cvr_updateRemindDate", "");
                EditorPrefs.SetString("cvr_skipVersion", "");
                CheckForUpdates();
            }


            //=========================
            //version
            //=========================

            if (GUILayout.Button("Save"))
            {
                SaveSettings();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: " + Core.SDK_Version);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Instance == null) { Instance = GetWindow<CognitiveVR_Settings>(true, "cognitiveVR Settings"); }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (lastSdkUpdateDate.Year < 100)
            {
                if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), out Instance.lastSdkUpdateDate))
                {
                    //Instance.updateDate = System.DateTime.Parse(EditorPrefs.GetString("cvr_updateDate", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    lastSdkUpdateDate.AddYears(600);
                }
            }
            if (lastSdkUpdateDate.Year > 1000)
            {
                GUILayout.Label("Last Updated: " + lastSdkUpdateDate.ToShortDateString());
            }
            else
            {
                GUILayout.Label("Last Updated: Never");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public void RequestLogin(string userEmail, string password)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                Debug.LogError("Missing Username!");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                Debug.LogError("Missing Password!");
                return;
            }

            var url = "https://api.cognitivevr.io/sessions";
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            string json = "{\"email\":\"" + userEmail + "\",\"password\":\"" + password + "\"}";
            byte[] bytes = new System.Text.UTF8Encoding(true).GetBytes(json);

            loginRequest = new UnityEngine.WWW(url, bytes, headers);

            Debug.Log("cognitiveVR Request Login");

            EditorApplication.update += CheckLoginResponse;
        }

        WWW loginRequest;

        void CheckLoginResponse()
        {
            if (!loginRequest.isDone)
            {
                //check for timeout
            }
            else
            {
                if (!string.IsNullOrEmpty(loginRequest.error))
                {
                    //loginResponse = "Could not log in!";
                    Debug.LogWarning("Could not log in to cognitiveVR SDK. Error: " + loginRequest.error);

                }
                if (!string.IsNullOrEmpty(loginRequest.text))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(loginRequest.text))
                        {
                            GetPreferences().UserData = JsonUtility.FromJson<Json.UserData>(loginRequest.text);

                            foreach (var v in loginRequest.responseHeaders)
                            {
                                if (v.Key == "SET-COOKIE")
                                {
                                    GetPreferences().fullToken = v.Value;
                                    //split semicolons. ignore everything except split[0]
                                    string[] split = v.Value.Split(';');
                                    GetPreferences().sessionID = split[0].Substring(18);
                                }
                            }

                            AssetDatabase.SaveAssets();
                        }
                        else
                        {
                            Debug.Log("cognitiveVR login request returned empty string!");
                        }
                    }
                    catch (System.Exception e)
                    {
                        //this can rarely happen when json is not formatted correctly
                        Debug.LogError("Cannot log in to cognitiveVR. Error: " + e.Message);
                        throw;
                    }
                }

                EditorApplication.update -= CheckLoginResponse;
                Repaint();
            }
        }

        WWW NewProductRequest;
        public void RequestNewProduct(string productName)
        {
            //why is new product not null here? should only be assigned a value later in this funciton
            if (NewProduct != null && !string.IsNullOrEmpty(NewProduct.name))
            {
                Debug.LogError("Request new product failed. Currently trying to create product " + NewProduct.name + " " + productName);
                return;
            }

            var url = "https://api.cognitivevr.io/organizations/" + GetPreferences().SelectedOrganization.prefix + "/products";
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");
            headers.Add("Cookie", GetPreferences().fullToken);

            productName = productName.Replace("\"", "\\\"");

            System.Text.StringBuilder json = new System.Text.StringBuilder();
            json.Append("{");
            //json.Append("\"sessionId\":\"" + GetPreferences().sessionID + "\",");
            //json.Append("\"organizationName\":\"" + GetPreferences().SelectedOrganization.prefix + "\",");
            json.Append("\"productName\":\"" + productName + "\"");
            json.Append("}");

            byte[] bytes = new System.Text.UTF8Encoding(true).GetBytes(json.ToString());
            NewProductRequest = new UnityEngine.WWW(url, bytes, headers);

            NewProduct = new Json.Product();
            NewProduct.name = productName;
            NewProduct.orgId = GetPreferences().SelectedOrganization.id;
            NewProduct.customerId = GetPreferences().SelectedOrganization.prefix + "-" + productName;
            NewProduct.customerId = NewProduct.customerId.ToLower().Replace(" ", "");

            EditorApplication.update += UpdateNewProductRequest;
        }

        Json.Product NewProduct;

        public void UpdateNewProductRequest()
        {
            if (!NewProductRequest.isDone)
            {
                //www timeout defaults to ~10 seconds
            }
            else
            {
                if (!string.IsNullOrEmpty(NewProductRequest.error))
                {
                    Debug.LogError("New Product Error: " + NewProductRequest.error);
                }
                else if (!string.IsNullOrEmpty(NewProductRequest.text))
                {
                    Debug.Log("New Product Response: " + NewProductRequest.text);

                    Json.Product responseProduct = JsonUtility.FromJson<Json.Product>(NewProductRequest.text);
                    responseProduct.orgId = NewProduct.orgId;

                    var AddedProduct = GetPreferences().UserData.AddProduct(responseProduct.name, responseProduct.customerId, responseProduct.orgId, responseProduct.id);
                    GetPreferences().SelectedProduct = AddedProduct;
                    SaveSettings();
                }
                else
                {
                    Debug.LogWarning("New Product Response has no text!");
                }

                NewProduct = null;
                EditorApplication.update -= UpdateNewProductRequest;
                Repaint();
            }
        }

        static WWW checkForUpdatesRequest;
        static void CheckForUpdates()
        {
            checkForUpdatesRequest = new UnityEngine.WWW(SdkVersionUrl);
            EditorApplication.update += UpdateCheckForUpdates;
        }

        public static void UpdateCheckForUpdates()
        {
            if (!checkForUpdatesRequest.isDone)
            {
                //check for timeout
            }
            else
            {
                if (!string.IsNullOrEmpty(checkForUpdatesRequest.error))
                {
                    Debug.Log("Check for cognitiveVR SDK version update error: " + checkForUpdatesRequest.error);
                }

                if (!string.IsNullOrEmpty(checkForUpdatesRequest.text))
                {
                    //TODO check that the text is in the right format

                    string[] split = checkForUpdatesRequest.text.Split('|');

                    var version = split[0];
                    string summary = split[1];

                    if (version != null)
                    {
                        if (EditorPrefs.GetString("cvr_skipVersion") == version)
                        {
                            //skip this version. limit this check to once a day
                            EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            CognitiveVR_UpdateSDKWindow.Init(version, summary);
                        }
                    }
                }

                EditorApplication.update -= UpdateCheckForUpdates;
            }
        }

        public void Logout()
        {
            var prefs = GetPreferences();
            prefs.UserData = Json.UserData.Empty;
            prefs.sessionID = string.Empty;
            prefs.UserName = string.Empty;
            password = string.Empty;
            prefs.SelectedOrganization = new Json.Organization();
            prefs.SelectedProduct = new Json.Product();
            //you do not need to stay logged in to keep your customerid for your product.
            AssetDatabase.SaveAssets();
        }

        public void SaveSettings()
        {
            CognitiveVR.CognitiveVR_Preferences prefs = CognitiveVR_Settings.GetPreferences();
            if (prefs.SelectedProduct != null && testprodSelection > -1)
            {
                prefs.CustomerID = prefs.SelectedProduct.customerId;
                if (testprodSelection == 0)
                    prefs.CustomerID += "-test";
                if (testprodSelection == 1)
                    prefs.CustomerID += "-prod";
            }

            EditorUtility.SetDirty(prefs);
            AssetDatabase.SaveAssets();

            if (EditorPrefs.GetString("cvr_version") != CognitiveVR.Core.SDK_Version)
            {
                EditorPrefs.SetString("cvr_version", CognitiveVR.Core.SDK_Version);
                EditorPrefs.SetString("cvr_updateDate", System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture));
                lastSdkUpdateDate = System.DateTime.UtcNow;
            }
        }

        public string[] GetUserOrganizations()
        {
            List<string> organizationNames = new List<string>();

            for (int i = 0; i < GetPreferences().UserData.organizations.Length; i++)
            {
                organizationNames.Add(GetPreferences().UserData.organizations[i].name);
            }

            return organizationNames.ToArray();
        }

        public string[] GetVRProductNames(string organization)
        {
            List<string> productNames = new List<string>();

            CognitiveVR.CognitiveVR_Preferences prefs = GetPreferences();

            Json.Organization org = prefs.GetOrganization(organization);
            if (org == null) { return productNames.ToArray(); }

            for (int i = 0; i < GetPreferences().UserData.products.Length; i++)
            {
                if (prefs.UserData.products[i].orgId == org.id)
                    productNames.Add(prefs.UserData.products[i].name);
            }

            return productNames.ToArray();
        }

        public void SetPlayerDefine(List<string> newDefines)
        {
            //get all scripting define symbols
            string s = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string[] symbols = s.Split(';');


            int cvrDefineCount = 0;
            int newDefineContains = 0;
            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].StartsWith("CVR_"))
                {
                    cvrDefineCount++;
                }
                if (newDefines.Contains(symbols[i]))
                {
                    newDefineContains++;
                }
            }

            if (newDefineContains == cvrDefineCount && cvrDefineCount != 0)
            {
                //all defines already exist
                return;
            }

            //remove all CVR_ symbols
            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].Contains("CVR_"))
                {
                    symbols[i] = "";
                }
            }

            //rebuild symbols
            string alldefines = "";
            for (int i = 0; i < symbols.Length; i++)
            {
                if (!string.IsNullOrEmpty(symbols[i]))
                {
                    alldefines += symbols[i] + ";";
                }
            }

            foreach (string define in newDefines)
            {
                alldefines += define + ";";
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, alldefines);

        }

        /// <summary>
        /// Gets the cognitivevr_preferences or creates and returns new default preferences
        /// </summary>
        /// <returns>Preferences</returns>
        public static CognitiveVR_Preferences GetPreferences()
        {
            CognitiveVR_Preferences asset = AssetDatabase.LoadAssetAtPath<CognitiveVR_Preferences>("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CognitiveVR_Preferences>();
                AssetDatabase.CreateAsset(asset, "Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
                AssetDatabase.Refresh();
            }
            return asset;
        }

        public bool IsUserLoggedIn()
        {
            if (string.IsNullOrEmpty(GetPreferences().sessionID))
            {
                return false;
            }
            return true;
        }

        static GUIStyle headerStyle;
        public static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle(EditorStyles.largeLabel);
                    headerStyle.fontSize = 14;
                    headerStyle.alignment = TextAnchor.UpperCenter;
                    headerStyle.fontStyle = FontStyle.Bold;
                    headerStyle.richText = true;
                }
                return headerStyle;
            }
        }

        public static string GhostTextField(string ghostText, string label, string actualText)
        {
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(actualText))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

                EditorGUILayout.TextField(label, ghostText, style);
                return "";
            }
            else
            {
                actualText = EditorGUILayout.TextField(label, actualText);
            }
            return actualText;
        }

        public static string GhostPasswordField(string ghostText, string label, string actualText)
        {
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(actualText))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

                EditorGUILayout.TextField(label, ghostText, style);
                return "";
            }
            else
            {
                actualText = EditorGUILayout.PasswordField(label, actualText);
            }

            return actualText;
        }

        public static void UserStartupBox(string number, bool showCheckbox)
        {
            var greencheck = EditorGUIUtility.FindTexture("Collab");

            if (showCheckbox)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Box(greencheck, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Box(CognitiveVR_Settings.GreyTextColorString + number + "</color>", GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}