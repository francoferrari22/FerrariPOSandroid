# FerrariPOS License Admin Android — OFFLINE

Administrador local de licencias FerrariPOS.

Flujo:
1. En la PC FerrariPOS se obtiene el Machine ID.
2. Se copia ese Machine ID al Android.
3. El administrador indica los días o Permanente.
4. Android firma localmente un token FPOS-LIC-3.
5. Se copia/envía el token (WhatsApp, etc.).
6. En Windows se pega en Configuración → Licencia y se aplica.

No necesita Internet, servidor, API, Cloudflare ni conexión con FerrariPOS para generar el token.

NOTA DE SEGURIDAD:
Para poder firmar completamente offline, esta aplicación contiene el material privado de firma dentro del APK. Esto permite operar sin servidor, pero también significa que una persona con conocimientos técnicos podría extraerlo del APK. Para máxima seguridad comercial, la variante de producción debería utilizar un dispositivo/servicio de firma controlado por el desarrollador.
