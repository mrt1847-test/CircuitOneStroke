using UnityEngine;

namespace CircuitOneStroke.View
{
    /// <summary>
    /// 리소스 없이 런타임 생성하는 procedural 스프라이트.
    /// 전구/스위치/다이오드/게이트 마커용.
    /// </summary>
    public static class ProceduralSprites
    {
        private static Sprite _circle;
        private static Sprite _bulbShape;
        private static Sprite _switchLever;
        private static Sprite _diodeTriangleBar;
        private static Sprite _gateLock;

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                    _circle = CreateCircle(64);
                return _circle;
            }
        }

        /// <summary>전구 실루엣: 원 + 아래 작은 stem. 형태로 전구 인지.</summary>
        public static Sprite BulbShape
        {
            get
            {
                if (_bulbShape == null)
                    _bulbShape = CreateBulbShape(64);
                return _bulbShape;
            }
        }

        /// <summary>스위치 레버: 가로 바. 전구와 형태 차별.</summary>
        public static Sprite SwitchLever
        {
            get
            {
                if (_switchLever == null)
                    _switchLever = CreateSwitchLever(64);
                return _switchLever;
            }
        }

        /// <summary>다이오드: 삼각형 + 바. 방향 인지용. 화면에서 20~28px 수준.</summary>
        public static Sprite DiodeTriangleBar
        {
            get
            {
                if (_diodeTriangleBar == null)
                    _diodeTriangleBar = CreateDiodeTriangleBar(64);
                return _diodeTriangleBar;
            }
        }

        /// <summary>전류 흐름용 텍스처. 점(spot)이 아닌 연속 스트릭(가운데 밝은 띠). LineRenderer Tile + UV 오프셋으로 "빛이 흐르는 선" 표현.</summary>
        public static Texture2D ElectricFlowTexture
        {
            get
            {
                if (_electricFlowTex == null)
                    _electricFlowTex = CreateElectricFlowTexture(256, 8);
                return _electricFlowTex;
            }
        }
        private static Texture2D _electricFlowTex;

        /// <summary>게이트 잠금: 자물쇠 형태. 끊김/잠금 느낌.</summary>
        public static Sprite GateLock
        {
            get
            {
                if (_gateLock == null)
                    _gateLock = CreateGateLock(48);
                return _gateLock;
            }
        }

        private static Sprite CreateCircle(int size)
        {
            var tex = new Texture2D(size, size);
            float cx = size * 0.5f;
            float r = cx - 1;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
                    tex.SetPixel(x, y, d <= r ? Color.white : Color.clear);
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateBulbShape(int size)
        {
            var tex = new Texture2D(size, size);
            float cx = size * 0.5f;
            float cy = size * 0.55f; // 위로 살짝
            float r = size * 0.38f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    bool inCircle = d <= r;
                    // 아래 stem: 좁은 직사각형
                    bool inStem = y < cy - r * 0.6f && Mathf.Abs(dx) < size * 0.12f && y > cy - r * 1.4f;
                    tex.SetPixel(x, y, (inCircle || inStem) ? Color.white : Color.clear);
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateSwitchLever(int size)
        {
            var tex = new Texture2D(size, size);
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float w = size * 0.45f;
            float h = size * 0.18f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool inBar = Mathf.Abs(x - cx) <= w && Mathf.Abs(y - cy) <= h;
                    tex.SetPixel(x, y, inBar ? Color.white : Color.clear);
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>다이오드: 오른쪽 삼각형 + 왼쪽 바. 방향 인지용.</summary>
        private static Sprite CreateDiodeTriangleBar(int size)
        {
            var tex = new Texture2D(size, size);
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    // 바: 왼쪽 -0.45 ~ -0.1 (정규화)
                    bool inBar = dx >= -size * 0.45f && dx <= -size * 0.05f && Mathf.Abs(dy) <= size * 0.2f;
                    // 삼각형: 오른쪽 뾰족 (dx > 0). 정점 = (cx+0.4*size, cy), 밑변 = dx = -0.1 ~ 0
                    float tx = (dx + size * 0.05f) / (size * 0.45f);
                    float ty = Mathf.Abs(dy) / (size * 0.4f);
                    bool inTriangle = tx >= 0 && tx <= 1f && ty <= 1f - tx;
                    tex.SetPixel(x, y, (inTriangle || inBar) ? Color.white : Color.clear);
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>자물쇠 형태. 끊김/잠금 느낌.</summary>
        private static Sprite CreateGateLock(int size)
        {
            var tex = new Texture2D(size, size);
            float cx = size * 0.5f;
            float cy = size * 0.52f;
            float r = size * 0.28f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    bool inShackle = dy > -r * 0.3f && dy < r * 1.1f && Mathf.Abs(dx) < size * 0.18f;
                    bool inBody = dy >= -r * 0.3f && dy < r && Mathf.Abs(dx) < r * 0.9f;
                    bool inKeyhole = dy < 0 && Mathf.Abs(dx) < size * 0.08f && dy > -r * 0.6f;
                    tex.SetPixel(x, y, ((inShackle || inBody) && !inKeyhole) ? Color.white : Color.clear);
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>점 무늬가 아닌 "연속 스트릭" 텍스처: 가운데 밝고 양끝으로 부드럽게 사라지는 띠 한 줄. Repeat + UV 스크롤 시 전기 흐름처럼 보임.</summary>
        private static Texture2D CreateElectricFlowTexture(int w, int h)
        {
            var tex = new Texture2D(w, h);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            float centerX = w * 0.5f;
            float falloff = w * 0.35f; // 띠가 부드럽게 사라지는 폭 (너무 짧으면 선처럼, 길면 넓게 퍼짐)
            for (int y = 0; y < h; y++)
            {
                float vy = 1f - Mathf.Abs((float)y / h - 0.5f) * 2f;
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - centerX);
                    float band = Mathf.Exp(-(dx * dx) / (falloff * falloff));
                    float a = (band * 0.75f + 0.25f) * vy;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
