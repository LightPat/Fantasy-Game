using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LightPat.Editor
{
    public class MassImportSettingsChange : MonoBehaviour
    {
        [MenuItem("Animation Tools/Mass Import Settings Change")]
        static void ChangeImportSettings()
        {
            Avatar srcAvatar = Selection.activeObject as Avatar;

            //string path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
            string path = "Assets/Models/Y Bot/Animations";

            foreach (string file in Directory.GetFiles(path))
            {
                AssetImporter assetImporter = AssetImporter.GetAtPath(file);
                ModelImporter modelImporter = (ModelImporter)assetImporter;
                if (modelImporter == null) { continue; }

                // Convert model rigs to humanoid
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                modelImporter.sourceAvatar = srcAvatar;
                modelImporter.animationCompression = ModelImporterAnimationCompression.Off;
                modelImporter.SaveAndReimport();

                // Change animation clip settings
                ModelImporterClipAnimation[] newClips = new ModelImporterClipAnimation[modelImporter.defaultClipAnimations.Length];

                int counter = 0;

                //foreach (ModelImporterClipAnimation clip in modelImporter.clipAnimations)
                //{
                //    clip.lockRootHeightY = true;
                //    newClips[counter] = clip;
                //    counter++;
                //}

                foreach (ModelImporterClipAnimation clip in modelImporter.defaultClipAnimations)
                {
                    clip.name = Path.GetFileNameWithoutExtension(file);
                    Debug.Log(clip.name);

                    string[] loopTerms = { "Walk", "Run", "Crouch", "Sprint", "Idle" };
                    foreach (string term in loopTerms)
                    {
                        if (clip.name.Contains(term))
                        {
                            clip.loopTime = true;
                            break;
                        }
                    }

                    if (clip.name.Contains("Jump") | clip.name.Contains("Fall") | clip.name.Contains("Land"))
                    {
                        clip.loopTime = false;
                        clip.lockRootRotation = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = true;
                        clip.keepOriginalOrientation = true;
                        clip.keepOriginalPositionXZ = true;
                        clip.heightFromFeet = false;
                        clip.keepOriginalPositionY = true;
                    }
                    else
                    {
                        clip.lockRootRotation = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = false;
                        clip.keepOriginalOrientation = true;
                        clip.keepOriginalPositionXZ = false;
                        clip.heightFromFeet = true;
                        clip.keepOriginalPositionY = false;
                    }

                    if (clip.name.Substring(clip.name.Length - 2) == "FL" | clip.name.Substring(clip.name.Length - 2) == "BR")
                    {
                        clip.rotationOffset = 45;
                    }
                    else if (clip.name.Substring(clip.name.Length - 2) == "FR" | clip.name.Substring(clip.name.Length - 2) == "BL")
                    {
                        clip.rotationOffset = -45;
                    }

                    newClips[counter] = clip;
                    counter++;
                }

                modelImporter.clipAnimations = newClips;
                modelImporter.SaveAndReimport();
            }
        }
    }
}