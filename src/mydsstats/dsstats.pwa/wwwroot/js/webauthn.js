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

    function copyDefined(value) {
        if (!value) {
            return value;
        }

        return Object.fromEntries(
            Object.entries(value).filter(([, entryValue]) => entryValue !== null && entryValue !== undefined));
    }

    function normalizeCredentialDescriptor(credential) {
        const descriptor = copyDefined({
            type: credential.type || "public-key",
            id: base64UrlToBuffer(credential.id),
            transports: credential.transports
        });

        if (!Array.isArray(descriptor.transports) || descriptor.transports.length === 0) {
            delete descriptor.transports;
        }

        return descriptor;
    }

    function normalizeAuthenticatorSelection(authenticatorSelection) {
        const selection = copyDefined(authenticatorSelection);
        if (!selection) {
            return selection;
        }

        if (selection.residentKey === "required") {
            selection.requireResidentKey = true;
        }

        return selection;
    }

    function normalizeCreateOptions(options) {
        const publicKey = copyDefined(options);
        publicKey.challenge = base64UrlToBuffer(publicKey.challenge);
        publicKey.user = copyDefined({ ...publicKey.user, id: base64UrlToBuffer(publicKey.user.id) });
        publicKey.authenticatorSelection = normalizeAuthenticatorSelection(publicKey.authenticatorSelection);
        if (!publicKey.authenticatorSelection) {
            delete publicKey.authenticatorSelection;
        }

        publicKey.excludeCredentials = (publicKey.excludeCredentials || []).map(normalizeCredentialDescriptor);
        if (publicKey.excludeCredentials.length === 0) {
            delete publicKey.excludeCredentials;
        }

        return { publicKey };
    }

    function normalizeGetOptions(options) {
        const publicKey = copyDefined(options);
        publicKey.challenge = base64UrlToBuffer(publicKey.challenge);
        publicKey.allowCredentials = (publicKey.allowCredentials || []).map(normalizeCredentialDescriptor);
        if (publicKey.allowCredentials.length === 0) {
            delete publicKey.allowCredentials;
        }

        return { publicKey };
    }

    function formatWebAuthnError(error, operation) {
        const name = error && error.name ? error.name : "";
        const message = error && error.message ? error.message : "";
        const lowerMessage = message.toLowerCase();

        if (name === "NotAllowedError") {
            return "Passkey " + operation + " was cancelled, timed out, or the browser could not access an authenticator.";
        }

        if (name === "InvalidStateError" || lowerMessage.includes("no longer, usable") || lowerMessage.includes("no longer usable")) {
            return "Firefox could not " + operation + " this passkey. Make sure Firefox is allowed to use your authenticator and that it supports passkeys, then try again.";
        }

        return message || "Passkey " + operation + " failed.";
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
            try {
                const credential = await navigator.credentials.create(normalizeCreateOptions(options));
                return publicKeyCredentialToJson(credential);
            } catch (error) {
                throw new Error(formatWebAuthnError(error, "create"));
            }
        },
        get: async function (options) {
            try {
                const credential = await navigator.credentials.get(normalizeGetOptions(options));
                return publicKeyCredentialToJson(credential);
            } catch (error) {
                throw new Error(formatWebAuthnError(error, "use"));
            }
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
