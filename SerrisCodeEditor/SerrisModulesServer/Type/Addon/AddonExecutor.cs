﻿using SerrisModulesServer.Items;
using SerrisModulesServer.Manager;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Storage;

namespace SerrisModulesServer.Type.Addon
{
    public enum AddonExecutorFuncTypes
    {
        main,
        onEditorStart,
        onEditorViewReady,
        whenModuleIsPinned
    }

    public class AddonExecutor
    {

        private ChakraSMS host;
        private int _id; private object _SCEELibs;

        public AddonExecutor(int ID, object SCEELibs)
        {
            _id = ID; _SCEELibs = SCEELibs;
            //InitializeExecutor(ID, FuncType, SCEELibs);
        }

        public void ExecutePersonalizedFunction(string function_name)
        {
            host = new ChakraSMS();

            host.Chakra.ProjectNamespace("SCEELibs.Editor");
            host.Chakra.ProjectNamespace("SCEELibs.Modules");
            host.Chakra.ProjectNamespace("SCEELibs.Modules.Type");
            host.Chakra.ProjectNamespace("SCEELibs.Tabs");

            IntializeChakraAndExecute(function_name);

        }

        public void ExecuteDefaultFunction(AddonExecutorFuncTypes FuncType)
        {
            host = new ChakraSMS();
            host.Chakra.ProjectNamespace("SCEELibs.Editor");
            host.Chakra.ProjectNamespace("SCEELibs.Modules");
            host.Chakra.ProjectNamespace("SCEELibs.Modules.Type");
            host.Chakra.ProjectNamespace("SCEELibs.Tabs");

            IntializeChakraAndExecute(FuncType.ToString());


        }

        private async void IntializeChakraAndExecute(string function_name)
        {
            /*
             * =============================
             * = ADDONS EXECUTOR VARIABLES =
             * =============================
             */


            host.Chakra.ProjectObjectToGlobal(_SCEELibs, "sceelibs");


            /*
             * ===========================
             * = ADDONS EXECUTOR CONTENT =
             * ===========================
             */


            InfosModule ModuleAccess = AsyncHelpers.RunSync(async () => await ModulesAccessManager.GetModuleViaIDAsync(_id));
            StorageFolder folder_module;

            if (ModuleAccess.ModuleSystem)
            {
                StorageFolder folder_content = AsyncHelpers.RunSync(async () => await Package.Current.InstalledLocation.GetFolderAsync("SerrisModulesServer")),
                    folder_systemmodules = AsyncHelpers.RunSync(async () => await folder_content.GetFolderAsync("SystemModules"));
                folder_module = AsyncHelpers.RunSync(async () => await folder_systemmodules.CreateFolderAsync(_id + "", CreationCollisionOption.OpenIfExists));
            }
            else
            {
                StorageFolder folder_content = AsyncHelpers.RunSync(async () => await ApplicationData.Current.LocalFolder.CreateFolderAsync("modules", CreationCollisionOption.OpenIfExists));
                folder_module = AsyncHelpers.RunSync(async () => await folder_content.CreateFolderAsync(_id + "", CreationCollisionOption.OpenIfExists));
            }

            foreach (string path in ModuleAccess.JSFilesPathList)
            {
                StorageFolder _folder_temp = folder_module; StorageFile _file_read = AsyncHelpers.RunSync(async () => await folder_module.GetFileAsync("main.js")); bool file_found = false; string path_temp = path;
                
                while (!file_found)
                {
                    if (path_temp.Contains(Path.AltDirectorySeparatorChar))
                    {
                        //Debug.WriteLine(path_temp.Split(Path.AltDirectorySeparatorChar).First());
                        _folder_temp = AsyncHelpers.RunSync(async () => await _folder_temp.GetFolderAsync(path_temp.Split(Path.AltDirectorySeparatorChar).First()));
                        path_temp = path_temp.Substring(path_temp.Split(Path.AltDirectorySeparatorChar).First().Length + 1);
                    }
                    else
                    {
                        _file_read = AsyncHelpers.RunSync(async () => await _folder_temp.GetFileAsync(path_temp));
                        file_found = true;
                        break;
                    }
                }

                try
                {
                    using (StreamReader reader = AsyncHelpers.RunSync(async () => new StreamReader(await _file_read.OpenStreamForReadAsync())))
                    {
                        host.Chakra.RunScript(AsyncHelpers.RunSync(async () => await reader.ReadToEndAsync()));
                    }
                }
                catch
                {
                    Debug.WriteLine("Erreur ! :(");
                }

            }

            StorageFile main_js = AsyncHelpers.RunSync(async () => await folder_module.GetFileAsync("main.js"));
            try
            {
                string code = AsyncHelpers.RunSync(async () => await FileIO.ReadTextAsync(main_js));
                host.Chakra.RunScript(code);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                host.Chakra.CallFunction(function_name);
            }
            catch { }
        }

    }
}
