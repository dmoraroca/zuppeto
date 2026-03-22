import { AuthUser } from '../models/auth-user.model';

export const AUTH_USERS_FAKE: AuthUser[] = [
  {
    id: 'admin-yeppet',
    email: 'admin@admin.adm',
    password: 'Admin123',
    name: 'Administrador YepPet',
    role: 'ADMIN',
    city: 'Barcelona',
    country: 'Espanya',
    bio: 'Accés intern per revisar arquitectura, permisos i evolució del producte.',
    avatarUrl: null,
    privacyAccepted: true
  },
  {
    id: 'user-yeppet',
    email: 'user@user.com',
    password: 'Admin123',
    name: 'Usuari de prova',
    role: 'USER',
    city: 'Madrid',
    country: 'Espanya',
    bio: 'Perfil fake per validar login, sessió, consentiments i manteniment d’usuari.',
    avatarUrl: null,
    privacyAccepted: false
  }
];
