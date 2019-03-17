using UIForia.Attributes;
using UIForia.Elements;

namespace Demo {

    [Template("TextDemo/TextDemo.xml")]
    public class TextDemo : UIElement {

        public string textBinding;

        public void SetText(string text) {
            textBinding = text;
        }

    }

}