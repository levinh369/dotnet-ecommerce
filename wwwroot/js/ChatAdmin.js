let adminId = "6b3a728b-7a04-414c-b450-3a2639ac029d";
let replyingToMessageId = null; // Lưu messageId của tin nhắn đang trả lời
let oldestMessageTime = null; // để load tin nhắn cũ hơn
let messages = []; // Lưu tất cả tin nhắn đã load để tra cứu khi reply
const adminConnection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

var chat = {
    currentConversations: new Set(), // lưu danh sách các conversationId đã join

    initChat: async function (conversationIdInt) {
        try {
            // Start connection nếu chưa có
            if (adminConnection.state === signalR.HubConnectionState.Disconnected) {
                await adminConnection.start();
                console.log("SignalR connection started");
            }

            // Join conversation nếu chưa join
            if (!this.currentConversations.has(conversationIdInt)) {
                await adminConnection.invoke("JoinConversation", conversationIdInt);
                this.currentConversations.add(conversationIdInt);
                console.log("Admin joined conversation:", conversationIdInt);
            }
        } catch (err) {
            console.error("Error initChat:", err);
        }
    },

    getMessages: async function (conversationIdInt, before = null) {
        try {
            let url = `/Chat/GetMessages?conversationId=${conversationIdInt}&take=5`;
            if (before) url += `&before=${encodeURIComponent(before)}`;
            const res = await fetch(url);
            messages = await res.json();
            
            const chatBox = document.getElementById(`chat-messages-${conversationIdInt}`);
            if (!chatBox) {
                console.error("Không tìm thấy chat box cho conversationId:", conversationIdInt);
                return;
            }

            if (!chatBox.dataset.scrollBound) {
                chatBox.addEventListener("scroll", () => {
                    if (chatBox.scrollTop === 0 && oldestMessageTime) {
                        chat.getMessages(conversationIdInt, oldestMessageTime);
                    }
                });
                chatBox.dataset.scrollBound = "true";
            }

            let isInitialLoad = !before;
            let oldScrollHeight = chatBox.scrollHeight;

            if (isInitialLoad) {
                chatBox.innerHTML = "";
            }

            // Render messages nếu có
            if (messages.length > 0) {
                if (messages.length > 4) {
                    oldestMessageTime = messages[4].createdAt;
                } else {
                    oldestMessageTime = messages[messages.length - 1].createdAt;
                }
                for (const msg of messages) {
                    await this.renderMessage(msg, false); // Load tin cũ, không cuộn
                }
            } else if (isInitialLoad) {
                // Fix: Nếu không có tin nhắn, thêm placeholder
                const noMessageDiv = document.createElement("div");
                noMessageDiv.className = "text-muted text-center p-3";
                noMessageDiv.innerHTML = "Chưa có tin nhắn nào";
                chatBox.appendChild(noMessageDiv);
                console.log("No messages, added placeholder");
                oldestMessageTime = null; // Ngăn load thêm
            }

            if (isInitialLoad) {
                chatBox.scrollTop = chatBox.scrollHeight; // Cuộn xuống cuối
            } else {
                const newScrollHeight = chatBox.scrollHeight;
                chatBox.scrollTop += newScrollHeight - oldScrollHeight; // Giữ vị trí khi load tin cũ
            }
        } catch (err) {
            console.error("Error getMessages:", err);
        }
    },

    renderMessage: async function (msg, shouldScrollToBottom = false) {
        const chatBox = document.getElementById(`chat-messages-${msg.conversationId}`);
        if (!chatBox) {
            console.error("Không tìm thấy chat box cho conversationId:", msg.conversationId);
            return;
        }

        const bubble = document.createElement("div");
        const role = msg.senderId === adminId ? "admin" : "buyer";
        bubble.className = `chat-message ${role}`;
        bubble.dataset.messageId = msg.messageId;

        const date = new Date(msg.createdAt);
        const now = new Date();
        const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        const msgDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());

        let timeString = "";
        if (msgDay.getTime() === today.getTime()) {
            timeString = date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
        } else {
            const datePart = date.toLocaleDateString("vi-VN");
            const timePart = date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" });
            timeString = `${datePart} ${timePart}`;
        }
        debugger;
        let replyHTML = "";
        if (msg.replyToMessageId) {
            debugger;
            let repliedMsg = messages.find(m => m.messageId === msg.replyToMessageId);
            if (!repliedMsg) {
                debugger;
                const res = await fetch(`/Chat/GetMessageById?messageId=${msg.replyToMessageId}`);
                const data = await res.json();
                repliedMsg = data.message;
            }
            if (repliedMsg) {
                let replyText = "";
                const sender = msg.senderName?.toLowerCase();
                const repliedSender = repliedMsg.senderName?.toLowerCase();
                if (sender === "admin") {
                    // Admin đang trả lời
                    if (repliedSender === "admin") {
                        replyText = "Bạn đã trả lời chính mình";
                    } else {
                        replyText = "Bạn đã trả lời " + repliedMsg.senderName;
                    }
                } else {
                    // User đang trả lời
                    if (repliedSender === "admin") {
                        replyText = msg.senderName + " đã trả lời bạn";
                    } else {
                        replyText = repliedMsg.senderName + " đã trả lời chính mình";
                    }
                }

                replyHTML = `
        <div class="message-reply">
            <span class="reply-author">${replyText}</span>
            <div class="reply-content">${repliedMsg.content}</div>
        </div>
    `;
            }
        }
        bubble.innerHTML = `
        ${replyHTML}
            <div class="message-content">${msg.content}</div>
            <div class="message-time">${timeString}</div>
            <div class="message-actions">
                <div class="dropdown">
                    <button class="btn btn-sm btn-link text-muted p-0" type="button" data-bs-toggle="dropdown">
                        <i class="bi bi-three-dots-vertical"></i>
                    </button>
                    <ul class="dropdown-menu">
                        <li><a class="dropdown-item" href="#" onclick="editMessage('${msg.messageId}', '${msg.content}')">Sửa</a></li>
                        <li><a class="dropdown-item text-danger" href="#" onclick="deleteMessage('${msg.messageId}')">Xóa</a></li>
                        <li><a class="dropdown-item" href="#" onclick="chat.replyMessage('${msg.messageId}', '${msg.content}', '${msg.conversationId}','${msg.senderName}')">Trả lời</a></li>
                    </ul>
                </div>
            </div>
        `;

        // Fix: Tùy thuộc vào shouldScrollToBottom, render product card và bubble
        if (msg.productId) {
            console.log("Rendering product card for msg.productId:", msg.productId, "shouldScrollToBottom:", shouldScrollToBottom);
            const productRes = await fetch(`/Product/GetChatProduct?productId=${msg.productId}`);
            const product = await productRes.json();
            if (product) {
                const productCard = document.createElement("div");
                productCard.className = "chat-product border rounded p-2 mb-2 bg-light";
                productCard.innerHTML = `
                    <a href="/Product/Detail/${product.productId}" target="_blank" class="text-decoration-none text-dark">
                    <div>
                        <div class="mb-1 text-muted" style="font-size:12px">Bạn đang hỏi về sản phẩm này</div>
                        <div class="d-flex">
                            <img src="${product.image}" style="width:60px;height:60px;object-fit:cover" class="me-2"/>
                            <div>
                                 <div style="font-size:14px"><strong>${product.name}</strong></div>
                <div class="text-danger fw-bold small text-start">
    ${product.price.toLocaleString("vi-VN")} VNĐ
</div>


                            </div>
                        </div>
                    </div>
                    </a>`;
                if (shouldScrollToBottom) {
                    // Tin nhắn mới: append product card trước, rồi bubble
                    chatBox.appendChild(productCard);
                    chatBox.appendChild(bubble);
                } else {
                    // Tin nhắn cũ: prepend bubble trước, rồi product card để product card ở trên
                    chatBox.prepend(bubble);
                    chatBox.prepend(productCard);
                    console.log("Prepended bubble then product card for old message, msg.productId:", msg.productId);
                }
            } else {
                console.warn("Product không tồn tại cho msg.productId:", msg.productId);
                // Nếu không có product, chỉ render bubble
                shouldScrollToBottom ? chatBox.appendChild(bubble) : chatBox.prepend(bubble);
            }
        } else {
            // Nếu không có productId, chỉ render bubble
            shouldScrollToBottom ? chatBox.appendChild(bubble) : chatBox.prepend(bubble);
        }

        // Chỉ cuộn xuống dưới nếu là tin nhắn mới
        if (shouldScrollToBottom) {
            chatBox.scrollTop = chatBox.scrollHeight;
        }
    },

    

    sendMessage: function (conversationIdInt) {
        const input = document.getElementById(`chat-input-${conversationIdInt}`);
        const content = input.value.trim();
        const replyContainer = document.querySelector(`#chat-widget-${conversationIdInt} .reply-container`);
        const replyTo = replyContainer?.dataset.replyingTo || null;
        if (!content) return;

        adminConnection.invoke("SendMessage", conversationIdInt, content, null, replyTo ? Number(replyTo) : null)
            .then(() => input.value = "")
            .catch(err => console.error(err.toString()));
    },
    isRead: function (conversationId) {
        $.ajax({
            url: '/Chat/ReadMessage',
            type: 'POST',
            data: { conversationId: conversationId },
            success: function (res) {
                if (res.success && !res.alreadyRead) {
                    $('#dot-' + conversationId).remove();
                    let countElem = $('#readCount');
                    let count = parseInt(countElem.text());
                    if (!isNaN(count) && count > 0) {
                        countElem.text(count - 1);
                    }
                }
            },
            error: function () {
                toastr.error('Không thể cập nhật trạng thái thông báo');
            }
        });
    },

    toggleChat: async function (conversationIdInt, buyerName, check) {
        const container = document.getElementById("chat-widgets-container");
        if (check) {
            chat.isRead(conversationIdInt);
        }
        if (document.getElementById(`chat-widget-${conversationIdInt}`)) {
            document.getElementById(`chat-widget-${conversationIdInt}`).style.display = "block";
            return;
        }
        const widget = document.createElement("div");
        widget.id = `chat-widget-${conversationIdInt}`;
        widget.className = "chat-window card shadow-lg border-0";
        
        widget.style.position = "fixed";
        widget.style.bottom = "20px";
        widget.style.right = `${20 + this.currentConversations.size * 460}px`;

        widget.innerHTML = `
        <div class="card-header bg-primary text-white d-flex justify-content-between align-items-center">
            <span>${buyerName}</span>
            <div class="d-flex align-items-center">
                <button type="button" class="btn-close btn-close-white btn-sm"
                        onclick="chat.closeChat(${conversationIdInt})"></button>
            </div>
        </div>
        <div id="chat-messages-${conversationIdInt}" class="card-body" style="height:300px; overflow-y:auto;"></div>
       <div class="card-footer">
        <div class="reply-container mb-2"></div> <!-- chỗ để hiện replyBox -->
        <div class="d-flex">
            <input type="text" id="chat-input-${conversationIdInt}" 
                   class="form-control me-2 chat-input" 
                   placeholder="Nhập tin nhắn..." />
            <button class="btn btn-primary" onclick="chat.sendMessage(${conversationIdInt})">Gửi</button>
        </div>
    </div>
    `;
        container.appendChild(widget);
        await this.getMessages(conversationIdInt);
        await this.initChat(conversationIdInt);
    },
    replyMessage: function (messageId, content, conversationId, senderName) {
        var name = "chính mình";
        if (senderName.toLowerCase() !== "admin")
            name = senderName;
        const replyContainer = document.querySelector(`#chat-widget-${conversationId} .reply-container`);
        replyContainer.dataset.replyingTo = messageId;
        replyContainer.innerHTML = `
        <div class="reply-box d-flex justify-content-between align-items-center p-2 border-start border-primary bg-light rounded">
            <div class="me-2">
                <div class="fw-bold small">Đang trả lời ${name}</div >
                <div class="text-truncate small text-muted text-start" style="max-width:200px; text-align:left !important;">
    ${content}
</div>

            </div>
            <button type="button" class="btn btn-sm btn-link text-danger p-0" onclick="chat.cancelReply(${conversationId})">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>
    `;
    },

    cancelReply: function (conversationId) {
        const footer = document.querySelector(`#chat-widget-${conversationId} .card-footer`);
        const replyBox = footer.querySelector(".reply-box");
        if (replyBox) replyBox.remove();
        const replyContainer = document.querySelector(`#chat-widget-${conversationId} .reply-container`);
        if (replyContainer) {
            replyContainer.removeAttribute("data-replying-to"); // bỏ trạng thái reply
            replyContainer.innerHTML = ""; // xóa UI box trả lời
        }
    },
    closeChat: function (conversationIdInt) { 
        const widget = document.getElementById(`chat-widget-${conversationIdInt}`);
        if (widget) {
            widget.remove(); // xóa hẳn khỏi DOM
            this.currentConversations.delete(conversationIdInt);
        }
    },
    repositionChats: function () {
        const widgets = document.querySelectorAll("[id^='chat-widget-']");
        widgets.forEach((w, index) => {
            w.style.right = `${20 + index * 340}px`;
        });
    },

    listChatUser: function () {
        $.ajax({
            url: "/Chat/ListChatUser",
            type: "get",
            success: function (result) {
                $('#modal-placeholder-chatUser').html(result);
                $('#chatModal').modal('show');
            },
            error: function () {
                alert("Lỗi tải dữ liệu");
            }
        });
    },
    loadIsreadCount: function () {
        $.get('/Chat/GetUnreadConversationCount', function (count) {
            $('#readCount').text(count);
        }).fail(function () {
            toastr.error("Không thể tải số thông báo chưa đọc");
        });
    },
    loadInputRead: function (elem) {
        const conversationIdInt = elem.id.replace("chat-input-", "");
        chat.isRead(conversationIdInt);
    }

};
async function joinAllConversations() {
    const res = await fetch("/Chat/GetAllConversations");
    const convs = await res.json(); // list các conversationId mà admin có
    for (let conv of convs) {
        await adminConnection.invoke("JoinConversation", conv.id);
    }
}
document.addEventListener("DOMContentLoaded", async () => {
    try {
        await adminConnection.start();
        console.log("SignalR connected as Admin");

        await joinAllConversations(); // join tất cả các conv ngay khi load
        console.log("Admin đã join tất cả conversation");
    } catch (err) {
        console.error("Lỗi khi connect SignalR:", err);
    }
});

// Lắng nghe tin nhắn mới từ SignalR (cho tất cả conv)
adminConnection.on("ReceiveMessage", async function (msg) {
    chat.loadIsreadCount();
    console.log("Received message:", msg);
    if (!document.getElementById(`chat-widget-${msg.conversationId}`)) {
        chat.toggleChat(msg.conversationId, msg.buyerName, false);
        console.log("Mở chat widget cho conversationId:", msg.conversationId);
        return;
    }
    if (msg.replyToMessageId) {
        let repliedMsg = messages.find(m => m.messageId === msg.replyToMessageId);
        if (!repliedMsg) {
            debugger;
            const res = await fetch(`/Chat/GetMessageById?messageId=${msg.replyToMessageId}`);
            const data = await res.json();
            repliedMsg = data.message;
            messages.push(repliedMsg);
        }
    }
    if (!messages.find(m => m.messageId === msg.messageId)) {
        messages.push(msg);
    }
    chat.renderMessage(msg,true);
    const chatBox = document.getElementById(`chat-messages-${msg.conversationId}`);
    if (chatBox) {
        chatBox.scrollTop = chatBox.scrollHeight;
    } else {
        console.error("Không tìm thấy chat box cho conversationId:", msg.conversationId);
    }
    if (msg.replyToMessageId) {
        chat.cancelReply();
    }
});
$(function () {
    chat.loadIsreadCount();
});
$(document).on("focus", ".chat-input", function () {
    chat.loadInputRead(this);
});


