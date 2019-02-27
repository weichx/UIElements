           
v2f LineVertex(appdata input) {
   v2f o;
   
   uint flags = input.uv1.y;
   
   #define prevNext input.uv2
   
   float2 prev = prevNext.xy;
   float2 next = prevNext.zw;
   float2 curr = input.vertex.xy;
   
   float strokeWidth = input.uv1.w;
   
   float aa = 0; //strokeWidth < 2 ? 1 : antialias;
   
   int dir = GetByte0(flags) * 2 - 1; // remap [0, 1] to [-1, 1]
   uint isNear = GetByte1(flags);
   uint isCap = GetByte2(flags);
   
   float w = (strokeWidth * 0.5) + aa;
   
   float2 v0 = normalize(curr - prev);
   float2 v1 = normalize(next - curr);
   
   float2 n0 = float2(-v0.y, v0.x);
   float2 n1 = float2(-v1.y, v1.x);
   
   float2 miter = normalize(n0 + n1);
            
   float miterLength = w / dot(miter, n1);
   float2 pos = float2(0, 0);
   o.color = input.color;
   o.uv = float4(w, w * dir, 0, 0);
   
   o.flags = float4(RenderType_Stroke, w * dir, aa, strokeWidth);

   if(isCap) {
        if(isNear) {
            pos = curr + w * n1 * dir; // * v1 + dir * w * n1;
        }
        else {
            pos = curr + w * n0 * dir; //v0 + dir * w * n0;
        }
   }
   else {
       pos = curr + (miter * miterLength * dir);
   }

   o.secondaryColor = fixed4(0, 0, 0, 0);
   o.vertex = UnityObjectToClipPos(float3(pos, input.vertex.z));
   return o;
}

fixed4 LineFragment(v2f i) {
   float thickness = i.flags.w;
   float aa = i.flags.z;
   float w = (thickness * 0.5) - aa;

   float d = abs(i.uv.y) - w;
   
   if(d <= 0) {
       return i.color;
   }

   d /= aa;
   float threshold = 1;
   float afwidth = length(float2(ddx(d), ddy(d)));
   float alpha = smoothstep(threshold - afwidth, threshold + afwidth, d);
   return fixed4(i.color.rgb, i.color.a * (1 - alpha));
}
