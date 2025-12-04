import { Button } from '@components/atoms/Button';
import { GlassCard } from '@components/atoms/GlassCard';
import { browserSupportsWebAuthn, startAuthentication, startRegistration } from '@simplewebauthn/browser';
import { setCredentials } from '@store/slices/authSlice';
import { AlertCircle, CheckCircle, Fingerprint, Key, Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useDispatch } from 'react-redux';
import { useLocation, useNavigate } from 'react-router-dom';

type AuthMode = 'login' | 'register';

export function Login() {
    const navigate = useNavigate();
    const location = useLocation();
    const dispatch = useDispatch();
    const [mode, setMode] = useState<AuthMode>('login');
    const [email, setEmail] = useState('');
    const [displayName, setDisplayName] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [webAuthnSupported, setWebAuthnSupported] = useState(false);

    // Get the original path the user was trying to access
    const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/';

    useEffect(() => {
        setWebAuthnSupported(browserSupportsWebAuthn());
    }, []);

    const handleBiometricLogin = async () => {
        setLoading(true);
        setError(null);

        try {
            // Start authentication
            const beginRes = await fetch('/api/auth/passkey/login/begin', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({}),
                credentials: 'include'
            });

            if (!beginRes.ok) {
                throw new Error('Failed to start authentication');
            }

            const options = await beginRes.json();

            // Trigger biometric prompt
            const credential = await startAuthentication(options);

            // Complete authentication
            const completeRes = await fetch('/api/auth/passkey/login/complete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(credential),
                credentials: 'include'
            });

            if (!completeRes.ok) {
                const err = await completeRes.json();
                throw new Error(err.error?.message || 'Authentication failed');
            }

            const result = await completeRes.json();

            // Store token in localStorage and Redux
            localStorage.setItem('accessToken', result.data.accessToken);
            localStorage.setItem('user', JSON.stringify(result.data.user));
            dispatch(setCredentials({
                token: result.data.accessToken,
                user: result.data.user
            }));

            setSuccess('Login successful!');
            setTimeout(() => navigate(from, { replace: true }), 500);

        } catch (err: any) {
            console.error('Biometric login error:', err);
            if (err.name === 'NotAllowedError') {
                setError('Biometric authentication was cancelled or denied');
            } else if (err.name === 'InvalidStateError') {
                setError('No passkey found. Please register first.');
                setMode('register');
            } else {
                setError(err.message || 'Authentication failed');
            }
        } finally {
            setLoading(false);
        }
    };

    const handleBiometricRegister = async () => {
        if (!email) {
            setError('Please enter your email');
            return;
        }

        setLoading(true);
        setError(null);

        try {
            // Start registration
            const beginRes = await fetch('/api/auth/passkey/register/begin', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, displayName: displayName || email.split('@')[0] }),
                credentials: 'include'
            });

            if (!beginRes.ok) {
                const err = await beginRes.json();
                throw new Error(err.error?.message || 'Failed to start registration');
            }

            const options = await beginRes.json();

            // Trigger biometric enrollment
            const credential = await startRegistration(options);

            // Complete registration
            const completeRes = await fetch('/api/auth/passkey/register/complete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(credential),
                credentials: 'include'
            });

            if (!completeRes.ok) {
                const err = await completeRes.json();
                throw new Error(err.error?.message || 'Registration failed');
            }

            const result = await completeRes.json();

            // Store token in localStorage and Redux
            localStorage.setItem('accessToken', result.data.accessToken);
            localStorage.setItem('user', JSON.stringify(result.data.user));
            dispatch(setCredentials({
                token: result.data.accessToken,
                user: result.data.user
            }));

            setSuccess('Passkey registered successfully!');
            setTimeout(() => navigate(from, { replace: true }), 500);

        } catch (err: any) {
            console.error('Biometric registration error:', err);
            if (err.name === 'NotAllowedError') {
                setError('Biometric registration was cancelled or denied');
            } else if (err.name === 'InvalidStateError') {
                setError('This device already has a passkey registered');
            } else {
                setError(err.message || 'Registration failed');
            }
        } finally {
            setLoading(false);
        }
    };

    if (!webAuthnSupported) {
        return (
            <div className="min-h-screen bg-background flex items-center justify-center p-4">
                <GlassCard variant="elevated" className="p-8 max-w-md w-full text-center">
                    <AlertCircle className="w-16 h-16 text-semantic-error mx-auto mb-4" />
                    <h1 className="text-2xl font-bold text-text-primary mb-2">Biometric Not Supported</h1>
                    <p className="text-text-secondary">
                        Your browser or device doesn't support biometric authentication (WebAuthn).
                        Please use a modern browser like Chrome, Safari, or Edge on a device with Touch ID, Face ID, or Windows Hello.
                    </p>
                </GlassCard>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-background flex items-center justify-center p-4">
            <div className="w-full max-w-md">
                {/* Logo */}
                <div className="text-center mb-8">
                    <div className="w-20 h-20 mx-auto mb-4 rounded-2xl bg-gradient-to-br from-accent-purple to-accent-cyan flex items-center justify-center">
                        <Key className="w-10 h-10 text-white" />
                    </div>
                    <h1 className="text-3xl font-bold text-text-primary" style={{ "width": "100%" }}>LifeOS</h1>
                    <p className="text-text-secondary mt-1">Your personal life operating system</p>
                </div>

                <GlassCard variant="elevated" glow="accent" className="p-8">
                    <h2 className="text-xl font-semibold text-text-primary text-center mb-6">
                        {mode === 'login' ? 'Welcome Back' : 'Create Account'}
                    </h2>

                    {/* Error Message */}
                    {error && (
                        <div className="mb-6 p-4 rounded-lg bg-semantic-error/20 border border-semantic-error/50 flex items-start gap-3">
                            <AlertCircle className="w-5 h-5 text-semantic-error shrink-0 mt-0.5" />
                            <p className="text-sm text-semantic-error">{error}</p>
                        </div>
                    )}

                    {/* Success Message */}
                    {success && (
                        <div className="mb-6 p-4 rounded-lg bg-semantic-success/20 border border-semantic-success/50 flex items-start gap-3">
                            <CheckCircle className="w-5 h-5 text-semantic-success shrink-0 mt-0.5" />
                            <p className="text-sm text-semantic-success">{success}</p>
                        </div>
                    )}

                    {mode === 'login' ? (
                        <>
                            {/* Biometric Login Button */}
                            <Button
                                onClick={handleBiometricLogin}
                                disabled={loading}
                                className="w-full py-4 text-lg"
                                icon={loading ? <Loader2 className="w-6 h-6 animate-spin" /> : <Fingerprint className="w-6 h-6" />}
                            >
                                {loading ? 'Authenticating...' : 'Sign in with Biometrics'}
                            </Button>

                            <p className="text-center text-text-tertiary text-sm mt-6">
                                Use Touch ID, Face ID, or Windows Hello to sign in
                            </p>

                            {/* Dev Login - Only in development */}
                            {import.meta.env.DEV && (
                                <div className="mt-4 pt-4 border-t border-background-hover">
                                    <Button
                                        onClick={async () => {
                                            setLoading(true);
                                            setError(null);
                                            try {
                                                const res = await fetch('/api/auth/dev-login', {
                                                    method: 'POST',
                                                    headers: { 'Content-Type': 'application/json' },
                                                    body: JSON.stringify({
                                                        email: 'dean@fynbos.dev',
                                                        displayName: 'Dean Martin'
                                                    })
                                                });
                                                const data = await res.json();
                                                if (data.data?.accessToken) {
                                                    localStorage.setItem('accessToken', data.data.accessToken);
                                                    localStorage.setItem('user', JSON.stringify(data.data.user));
                                                    dispatch(setCredentials({
                                                        token: data.data.accessToken,
                                                        user: data.data.user
                                                    }));
                                                    setSuccess('Dev login successful!');
                                                    setTimeout(() => navigate(from, { replace: true }), 500);
                                                } else {
                                                    setError('Dev login failed');
                                                }
                                            } catch (err: any) {
                                                setError(err.message || 'Dev login failed');
                                            } finally {
                                                setLoading(false);
                                            }
                                        }}
                                        disabled={loading}
                                        variant="secondary"
                                        className="w-full py-3 text-sm"
                                    >
                                        🔧 Dev Login (Skip Passkey)
                                    </Button>
                                </div>
                            )}

                            <div className="mt-8 pt-6 border-t border-background-hover text-center">
                                <p className="text-text-secondary text-sm">
                                    Don't have an account?{' '}
                                    <button
                                        onClick={() => { setMode('register'); setError(null); }}
                                        className="text-accent-purple hover:text-accent-purple/80 font-medium"
                                    >
                                        Register with Passkey
                                    </button>
                                </p>
                            </div>
                        </>
                    ) : (
                        <>
                            {/* Registration Form */}
                            <div className="space-y-4 mb-6">
                                <div>
                                    <label htmlFor="email" className="block text-sm font-medium text-text-secondary mb-2">
                                        Email Address
                                    </label>
                                    <input
                                        type="email"
                                        id="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        placeholder="you@example.com"
                                        className="w-full px-4 py-3 rounded-lg bg-background-secondary border border-background-hover
                             text-text-primary placeholder-text-tertiary
                             focus:outline-none focus:ring-2 focus:ring-accent-purple/50 focus:border-accent-purple"
                                    />
                                </div>
                                <div>
                                    <label htmlFor="displayName" className="block text-sm font-medium text-text-secondary mb-2">
                                        Display Name (optional)
                                    </label>
                                    <input
                                        type="text"
                                        id="displayName"
                                        value={displayName}
                                        onChange={(e) => setDisplayName(e.target.value)}
                                        placeholder="Your name"
                                        className="w-full px-4 py-3 rounded-lg bg-background-secondary border border-background-hover
                             text-text-primary placeholder-text-tertiary
                             focus:outline-none focus:ring-2 focus:ring-accent-purple/50 focus:border-accent-purple"
                                    />
                                </div>
                            </div>

                            <Button
                                onClick={handleBiometricRegister}
                                disabled={loading || !email}
                                className="w-full py-4 text-lg"
                                icon={loading ? <Loader2 className="w-6 h-6 animate-spin" /> : <Fingerprint className="w-6 h-6" />}
                            >
                                {loading ? 'Setting up...' : 'Register with Biometrics'}
                            </Button>

                            <p className="text-center text-text-tertiary text-sm mt-6">
                                You'll be prompted to use Touch ID, Face ID, or Windows Hello
                            </p>

                            <div className="mt-8 pt-6 border-t border-background-hover text-center">
                                <p className="text-text-secondary text-sm">
                                    Already have an account?{' '}
                                    <button
                                        onClick={() => { setMode('login'); setError(null); }}
                                        className="text-accent-purple hover:text-accent-purple/80 font-medium"
                                    >
                                        Sign in
                                    </button>
                                </p>
                            </div>
                        </>
                    )}
                </GlassCard>

                <p className="text-center text-text-tertiary text-xs mt-6">
                    Secure passwordless authentication powered by WebAuthn
                </p>
            </div>
        </div>
    );
}
