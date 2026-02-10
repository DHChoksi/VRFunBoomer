using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XanderDevelops.UI
{
    public class TextModifier : MonoBehaviour
    {
        [Header("STEP 1. Set the text to modify")]
        [SerializeField] private bool changeAllText = false;
        [SerializeField, Tooltip("Only texts with this font will be changed unless 'Change All Text' is enabled.")] private TMP_FontAsset textToChange;
        [SerializeField, Tooltip("Enable to change texts based on their current color.")] private bool selectByTextColor = false;
        [SerializeField, Tooltip("The color of the texts to select for modification.")] private Color textColorToChange = Color.white;
        private float colorTolerance = 0.01f;

        [Space(20)]
        [Header("STEP 2. Set your desired Text properties")]
        [SerializeField] private bool changeFont = true;
        [SerializeField, Tooltip("The font to apply to the selected texts.")] private TMP_FontAsset targetFont;
        
        [Space(20)]
        [SerializeField] private bool changeFontSize = false;
        [SerializeField, Tooltip("The font size to apply.")] private float fontSize = 36f;

        [Space(20)]
        [SerializeField] private bool changeFontColor = false;
        [SerializeField, Tooltip("The color to apply to the text.")] private Color fontColor = Color.white;

        [Space(20)]
        [SerializeField] private bool changeTextAlignment = false;
        [SerializeField, Tooltip("The alignment to apply to the text.")] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;

        [Space(20)]
        [SerializeField] private bool changeCharacterSpacing = false;
        [SerializeField, Tooltip("The character spacing to apply.")] private float characterSpacing = 0f;

        [Space(20)]
        [SerializeField] private bool changeFontStyle = false;
        [SerializeField, Tooltip("The font style to apply.")] private FontStyles fontStyle = FontStyles.Normal;

    #if UNITY_EDITOR
        [ContextMenu("Apply Text Modifications")]
        public void ApplyTextModifications()
        {
            TMP_Text[] textMeshPros = FindObjectsOfType<TMP_Text>();

            foreach (TMP_Text textMeshPro in textMeshPros)
            {
                Color textActualColor = textMeshPro.color;
                
                Material sharedMaterial = textMeshPro.fontSharedMaterial;
                if (sharedMaterial != null && sharedMaterial.HasProperty(ShaderUtilities.ID_FaceColor))
                {
                    textActualColor = sharedMaterial.GetColor(ShaderUtilities.ID_FaceColor);
                }

                if ((changeAllText || 
                     (textToChange != null && textMeshPro.font == textToChange) || 
                     (selectByTextColor && ColorsAreClose(textMeshPro.color, textColorToChange, colorTolerance))))
                {
                    if (changeFont) textMeshPro.font = targetFont;
                    if (changeFontSize) textMeshPro.fontSize = fontSize;
                    if (changeFontColor) textMeshPro.color = fontColor;
                    if (changeTextAlignment) textMeshPro.alignment = textAlignment;
                    if (changeCharacterSpacing) textMeshPro.characterSpacing = characterSpacing;
                    if (changeFontStyle) textMeshPro.fontStyle = fontStyle;
                }
            }

            UnityEditor.SceneView.RepaintAll();
        }

        private bool ColorsAreClose(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                Mathf.Abs(a.g - b.g) < tolerance &&
                Mathf.Abs(a.b - b.b) < tolerance &&
                Mathf.Abs(a.a - b.a) < tolerance;
        }
    #endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TextModifier))]
    public class TextModifierEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TextModifier textModifier = (TextModifier)target;

            if (GUILayout.Button("Apply Changes"))
            {
                textModifier.ApplyTextModifications();
            }
        }
    }
#endif
}