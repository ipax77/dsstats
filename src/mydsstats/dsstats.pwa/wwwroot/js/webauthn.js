(function () {
    function base64UrlToBuffer(value) {
        const base64 = value.replace(/-/g, "+").replace(/_/g, "/");
        const padded = base64.padEnd(base64.length + ((4 - base64.length % 4) % 4), "=");
        const binary = atob(padded);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes.buffer;
    }

    function bufferToBase64Url(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = "";
        for (const byte of bytes) {
            binary += String.fromCharCode(byte);
        }
        return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
    }

    function normalizeCreateOptions(options) {
        const publicKey = { ...options };
        publicKey.challenge = base64UrlToBuffer(publicKey.challenge);
        publicKey.user = { ...publicKey.user, id: base64UrlToBuffer(publicKey.user.id) };
        publicKey.excludeCredentials = (publicKey.excludeCredentials || []).map(credential => ({
            ...credential,
            id: base64UrlToBuffer(credential.id)
        }));
        return { publicKey };
    }

    function normalizeGetOptions(options) {
        const publicKey = { ...options };
        publicKey.challenge = base64UrlToBuffer(publicKey.challenge);
        publicKey.allowCredentials = (publicKey.allowCredentials || []).map(credential => ({
            ...credential,
            id: base64UrlToBuffer(credential.id)
        }));
        return { publicKey };
    }

    function publicKeyCredentialToJson(credential) {
        const response = credential.response;
        const json = {
            id: credential.id,
            rawId: bufferToBase64Url(credential.rawId),
            type: credential.type,
            clientExtensionResults: credential.getClientExtensionResults(),
            response: {}
        };

        if (response.attestationObject) {
            json.response.attestationObject = bufferToBase64Url(response.attestationObject);
        }

        if (response.authenticatorData) {
            json.response.authenticatorData = bufferToBase64Url(response.authenticatorData);
        }

        if (response.clientDataJSON) {
            json.response.clientDataJSON = bufferToBase64Url(response.clientDataJSON);
        }

        if (response.signature) {
            json.response.signature = bufferToBase64Url(response.signature);
        }

        if (response.userHandle) {
            json.response.userHandle = bufferToBase64Url(response.userHandle);
        }

        return json;
    }

    window.dsstatsInHousePasskeys = {
        isSupported: function () {
            return !!window.PublicKeyCredential && !!navigator.credentials;
        },
        create: async function (options) {
            const credential = await navigator.credentials.create(normalizeCreateOptions(options));
            return publicKeyCredentialToJson(credential);
        },
        get: async function (options) {
            const credential = await navigator.credentials.get(normalizeGetOptions(options));
            return publicKeyCredentialToJson(credential);
        }
    };

    window.dsstatsInHouseModal = {
        hide: function (id) {
            const element = document.getElementById(id);
            if (!element || !window.bootstrap) {
                return;
            }

            const modal = window.bootstrap.Modal.getOrCreateInstance(element);
            modal.hide();
        }
    };
})();
