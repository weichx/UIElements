using System;
using System.Collections.Generic;

namespace UIForia.Animation {

    public struct AnimationData {

        public string name;
        public string fileName;
        public AnimationOptions options;
        public IList<AnimationKeyFrame> frames;
        public IList<AnimationTrigger> triggers;

        public Action<StyleAnimationEvent> onStart;
        public Action<StyleAnimationEvent> onEnd;
        public Action<StyleAnimationEvent> onCanceled;
        public Action<StyleAnimationEvent> onCompleted;
        public Action<StyleAnimationEvent> onTick;

        public AnimationData(AnimationOptions options, IList<AnimationKeyFrame> frames = null, IList<AnimationTrigger> triggers = null) {
            this.options = options;
            this.triggers = null;
            this.onStart = null;
            this.onEnd = null;
            this.onCanceled = null;
            this.onCompleted = null;
            this.onTick = null;
            this.frames = frames;
            this.triggers = triggers;
            this.name = null;
            this.fileName = null;
        }

    }

}