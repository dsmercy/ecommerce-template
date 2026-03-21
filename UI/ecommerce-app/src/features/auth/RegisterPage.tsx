import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Loader2 } from 'lucide-react';

import { registerSchema, type RegisterFormData } from './schemas/authSchemas';
import { registerUser } from '@api/endpoints/auth';
import { useAuthStore } from '@stores/authStore';
import { logger } from '@lib/logger';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Label } from '@components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@components/ui/card';

export default function RegisterPage() {
  const navigate = useNavigate();
  const { setTokens, setUser } = useAuthStore();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({ resolver: zodResolver(registerSchema) });

  const registerMutation = useMutation({
    mutationFn: registerUser,
    onSuccess: (response) => {
      const data = response.data;
      if (!data) { toast.error('Registration failed — unexpected response.'); return; }
      setTokens(data.accessToken, data.refreshToken, data.refreshTokenExpiry);
      setUser({ userId: data.userId, name: data.name, email: data.email, role: data.role });
      logger.info('User registered', { userId: data.userId });
      toast.success(`Welcome, ${data.name}! Your account has been created.`);
      navigate('/', { replace: true });
    },
    onError: (error: unknown) => {
      const msg = (error as { response?: { data?: { message?: string } } })
        ?.response?.data?.message ?? 'Registration failed. Please try again.';
      toast.error(msg);
    },
  });

  function onSubmit(values: RegisterFormData) {
    registerMutation.mutate({ ...values, phone: values.phone || undefined });
  }

  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center bg-muted/30 px-4 py-12">
      <div className="w-full max-w-md">
        <Card className="shadow-lg">
          <CardHeader className="space-y-1 pb-4">
            <CardTitle className="text-2xl font-bold text-center">Create an account</CardTitle>
            <CardDescription className="text-center">
              Already have an account?{' '}
              <Link to="/login" className="font-medium text-primary hover:underline underline-offset-4">
                Sign in
              </Link>
            </CardDescription>
          </CardHeader>

          <CardContent>
            <form
              className="space-y-4"
              onSubmit={(e) => { void handleSubmit(onSubmit)(e); }}
              noValidate
            >
              {/* Full name */}
              <div className="space-y-2">
                <Label htmlFor="name">Full name</Label>
                <Input
                  id="name"
                  type="text"
                  autoComplete="name"
                  placeholder="Jane Smith"
                  aria-invalid={!!errors.name}
                  className={errors.name ? 'border-destructive focus-visible:ring-destructive' : ''}
                  {...register('name')}
                />
                {errors.name && (
                  <p className="text-xs text-destructive" role="alert">{errors.name.message}</p>
                )}
              </div>

              {/* Email */}
              <div className="space-y-2">
                <Label htmlFor="email">Email address</Label>
                <Input
                  id="email"
                  type="email"
                  autoComplete="email"
                  placeholder="you@example.com"
                  aria-invalid={!!errors.email}
                  className={errors.email ? 'border-destructive focus-visible:ring-destructive' : ''}
                  {...register('email')}
                />
                {errors.email && (
                  <p className="text-xs text-destructive" role="alert">{errors.email.message}</p>
                )}
              </div>

              {/* Password */}
              <div className="space-y-2">
                <Label htmlFor="password">Password</Label>
                <Input
                  id="password"
                  type="password"
                  autoComplete="new-password"
                  placeholder="••••••••"
                  aria-invalid={!!errors.password}
                  className={errors.password ? 'border-destructive focus-visible:ring-destructive' : ''}
                  {...register('password')}
                />
                {errors.password ? (
                  <p className="text-xs text-destructive" role="alert">{errors.password.message}</p>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    Min 8 characters, one uppercase letter, one digit.
                  </p>
                )}
              </div>

              {/* Phone (optional) */}
              <div className="space-y-2">
                <Label htmlFor="phone">
                  Phone <span className="text-muted-foreground font-normal">(optional)</span>
                </Label>
                <Input
                  id="phone"
                  type="tel"
                  autoComplete="tel"
                  placeholder="+1 555 000 0000"
                  aria-invalid={!!errors.phone}
                  className={errors.phone ? 'border-destructive focus-visible:ring-destructive' : ''}
                  {...register('phone')}
                />
                {errors.phone && (
                  <p className="text-xs text-destructive" role="alert">{errors.phone.message}</p>
                )}
              </div>

              <Button type="submit" className="w-full" disabled={registerMutation.isPending}>
                {registerMutation.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
                {registerMutation.isPending ? 'Creating account…' : 'Create account'}
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
