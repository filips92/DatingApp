import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { User } from '../_models/user';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;

  constructor(
    private http: HttpClient
  ) { }

  getUsersWithRoles() {
    return this.http.get(this.baseUrl + 'admin/usersWithRoles');
  }
  updateUserRoles(user: User, roles: {}) {
    return this.http.post(this.baseUrl + 'admin/editRoles/' + user.userName, roles);
  }
  getPhotos() {
    return this.http.get(this.baseUrl + 'admin/photosForModeration');
  }
  acceptPhoto(id: number) {
    return this.http.post(this.baseUrl + 'admin/photos/' + id + '/accept', {});
  }
  rejectPhoto(id: number) {
    return this.http.post(this.baseUrl + 'admin/photos/' + id + '/reject', {});
  }
}
