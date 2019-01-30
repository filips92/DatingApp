import { Component, OnInit } from '@angular/core';
import { AdminService } from 'src/app/_services/admin.service';
import { Photo } from 'src/app/_models/photo';
import { AlertifyService } from 'src/app/_services/alertify.service';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {
  photos: Photo[];

  constructor(
    private adminService: AdminService,
    private alertify: AlertifyService
  ) { }

  ngOnInit() {
    this.getPhotosForModification();
  }

  getPhotosForModification() {
    this.adminService.getPhotos().subscribe((res: Photo[]) => {
      this.photos = res;
    }, error => {
      this.alertify.error(error);
    });
  }

  acceptPhoto(id: number) {
    this.adminService.acceptPhoto(id).subscribe(() => {
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
    }, error => {
      this.alertify.error(error);
    });
  }

  rejectPhoto(id: number) {
    this.adminService.rejectPhoto(id).subscribe(() => {
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
    }, error => {
      this.alertify.error(error);
    });
  }
}
