var AppAccount = {
    init: function () {
        $('#changePasswordForm').on('submit', function (e) {
            e.preventDefault();
            AppAccount.changePassword();
        });
    },

    changePassword: function () {
        var formData = $('#changePasswordForm').serialize();

        $.ajax({
            type: 'POST',
            url: '/Account/changePassWord',
            data: formData,
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message);
                    $('#changePasswordForm')[0].reset();
                } else {
                    toastr.error(response.message);
                }
            },
            error: function () {
                toastr.error("Lỗi hệ thống.");
            }
        });
    },

    successReset: function (message) {
        if (message && message !== '') {
            Swal.fire({
                icon: 'success',
                title: message,
                showConfirmButton: false,
                timer: 2500
            });

            setTimeout(function () {
                window.location.href = '/Account/Login';
            }, 2500);
        }
    },
    editUser: function () {
        var form = $('#profileForm');
        if (!form.valid()) return;
        var formData = new FormData(form[0]);
        $("#globalLoading").show();
        $.ajax({
            url: '/Account/EditUserAccount',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                if (res.success) {
                    $("#globalLoading").hide();
                    toastr.success(res.message);
                    if (res.avatarUrl) {
                        const headerImg = document.getElementById("avatarHeaderImg");
                        if (headerImg) {
                            headerImg.src = res.avatarUrl + '?v=' + new Date().getTime();
                            localStorage.setItem("avatarUrl", res.avatarUrl);
                        }
                    }
                } else {
                    $("#globalLoading").hide();
                    toastr.error(res.message);
                }
            },
            error: function () {
                toastr.error("Lỗi tải trang");
            }
        });
    },
    
};

$(function () {
    AppAccount.init();
});
$(document).on('submit', '#profileForm', function (e) {
    e.preventDefault(); // Ngăn form submit mặc định
    AppAccount.editUser(); // Gọi hàm ajax
});