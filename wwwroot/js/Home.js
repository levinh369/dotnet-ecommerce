$(document).ready(function () {
    loadData(1); // Tải 4 sản phẩm đầu tiên khi trang vừa load
});

function loadData(pageIndex) {
    $.ajax({
        url: "/Home/ListData",
        type: "get",
        data: { page: pageIndex }, // Gửi pageIndex lên Controller
        success: function (result) {
            $("#product-list").html(result); // Gắn HTML trả về vào div
        },
        error: function () {
            alert("Lỗi tải dữ liệu");
        }
    });
}
$(document).on("mouseenter", "#categoryMenu li.dropdown", function () {
    let li = $(this);
    let id = li.data("id");
    let subMenu = li.children("ul");
   
    // chỉ load 1 lần thôi
    if (subMenu.is(":empty")) {
        $.get("/Home/GetCategories", { parentId: id }, function (data) {
            let html = "";
            data.forEach(function (c) {
                html += `
                    <li class="list-group-item dropdown" data-id="${c.categoryId}">
                        <a href="#">${c.categoryName}</a>
                        ${c.hasChildren ? '<ul class="list-group d-none"></ul>' : ''}
                    </li>`;
            });
            subMenu.html(html);
        });
    }

    subMenu.removeClass("d-none"); // hiện submenu
});

// rời chuột thì ẩn submenu
$(document).on("mouseleave", "#categoryMenu li.dropdown", function () {
    $(this).children("ul").addClass("d-none");
});

