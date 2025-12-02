let conversationId = null;
let SendproductId = null;
let oldestMessageTime = null;
let replyingToMessageId = null;
let check = false;
let messages=[];
const buyerConnection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

var chat = {
    initChat: async function (buyerId) {
        if (conversationId) return; // đã init rồi, không tạo lại
        const res = await fetch('/Chat/StartConversation', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ buyerId: buyerId })
        });

        const data = await res.json();
        conversationId = data.conversationId;

        if (buyerConnection.state !== signalR.HubConnectionState.Connected) {
            await buyerConnection.start();
        }
        buyerConnection.invoke("JoinConversation", conversationId);

        console.log("Buyer joined conversation:", conversationId);
    },
    sendMessage: function () {
        const content = document.getElementById("chat-input").value.trim();
        if (!content || !conversationId) return;
        debugger;
        const chatBox = document.getElementById("chat-messages");
        if (check) {
            check = false;
        } else {
            SendproductId = null;
        }
        buyerConnection.invoke("SendMessage", conversationId, content, SendproductId, replyingToMessageId ? Number(replyingToMessageId) : null )
            .then(() => {
                document.getElementById("chat-input").value = "";
                chatBox.scrollTop = chatBox.scrollHeight;
            })
            .catch(err => console.error(err.toString()));
    },
    closeChat: function (conversationIdInt) {
        const widget = document.getElementById(`chatWindow`);
        if (widget) {
            widget.style.display = "none";

        }
    },
    loadMessages: async function (conversationId, before = null, productId) {
        let url = `/Chat/GetMessages?conversationId=${conversationId}&take=5`;
        if (before) url += `&before=${encodeURIComponent(before)}`;
        const res = await fetch(url);
        messages = await res.json();
        const chatBox = document.getElementById("chat-messages");
        if (chatBox && !chatBox.dataset.scrollBound) {
            chatBox.addEventListener("scroll", () => {
                if (chatBox.scrollTop === 0) {
                    chat.loadMessages(conversationId, oldestMessageTime);
                }
            }); chatBox.dataset.scrollBound = "true"; // tránh gắn nhiều lần 
        }
        let isInitialLoad = !before;
        let oldScrollHeight = chatBox.scrollHeight;

        if (isInitialLoad) {
            chatBox.innerHTML = "";
        }
        const adminId = "6b3a728b-7a04-414c-b450-3a2639ac029d"; // cố định
        if (messages.length > 0) {
            if (messages.length > 4) {
                oldestMessageTime = messages[4].createdAt;
            } else {
                oldestMessageTime = messages[messages.length - 1].createdAt;
            }
            
            for (const msg of messages) {
                const bubble = document.createElement("div");
                const role = msg.senderId === adminId ? "admin" : "buyer";
                bubble.className = `chat-message ${role}`;

                const date = new Date(msg.createdAt);
                const now = new Date();

                // reset giờ phút giây để chỉ so sánh ngày
                const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
                const msgDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());

                let timeString = "";
                if (msgDay.getTime() === today.getTime()) {
                    // 👉 Hôm nay: chỉ hiển thị giờ:phút
                    timeString = date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
                } else {
                    // 👉 Ngày cũ: hiển thị dd/MM HH:mm:ss
                    const datePart = date.toLocaleDateString("vi-VN");
                    const timePart = date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" });
                    timeString = `${datePart} ${timePart}`;
                }
                let replyHTML = "";
                if (msg.replyToMessageId) {
                    let repliedMsg = messages.find(m => m.messageId === msg.replyToMessageId);
                    if (!repliedMsg) {
                        debugger;
                        const res = await fetch(`/Chat/GetMessageById?messageId=${msg.replyToMessageId}`);
                        const data = await res.json();
                        repliedMsg = data.message;
                    }
                    debugger;
                    if (repliedMsg) {
                        let replyText = "";

                        if (msg.senderName.toLowerCase() === "admin") {
                            // Admin đang trả lời
                            if (repliedMsg.senderName.toLowerCase() === "admin") {
                                replyText = "Admin đã trả lời chính mình";
                            } else {
                                replyText = "Admin đã trả lời bạn";
                            }
                        } else {
                            // Người dùng đang trả lời
                            if (repliedMsg.senderName.toLowerCase() === "admin") {
                                replyText = "Bạn đã trả lời Admin";
                            } else {
                                replyText = "Bạn đã trả lời chính mình";
                            }
                        }

                        replyHTML = `
                <div class="message-reply">
                    <span class="reply-author"> ${replyText}</span>
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
                        <li><a class="dropdown-item" href="#" onclick="chat.replyMessage('${msg.messageId}', '${msg.content}','${msg.senderName}' )">Trả lời</a></li>
                    </ul>
                </div>
            </div>
            `;

                // 👇 Nếu có productId thì render card ngay sau bubble
                if (msg.productId) {
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
                <img src="${product.image}" 
                     style="width:60px;height:60px;object-fit:cover" 
                     class="me-2"/>
                <div>
                    <div style="font-size:14px"><strong>${product.name}</strong></div>
                    <div class="text-danger fw-bold" style="font-size:13px">
                        ${product.price.toLocaleString("vi-VN")} VNĐ
                    </div>
                </div>
            </div>
        </div>
        </a>`;
                        chatBox.prepend(bubble);
                        chatBox.prepend(productCard);
                    }
                }
                else
                    chatBox.prepend(bubble);
            }
        }
        if (productId) {
            const productRes = await fetch(`/Product/GetChatProduct?productId=${productId}`);
            const product = await productRes.json();
            if (product) {
                check = true;
                SendproductId = productId;
                const productCard = document.createElement("div");
                productCard.className = "chat-product border rounded p-2 mb-2 bg-light";
                productCard.innerHTML = `
    <div>
        <div class="mb-1 text-muted" style="font-size:12px">Bạn đang hỏi về sản phẩm này</div>
        <div class="d-flex">
            <img src="${product.image}" 
                 style="width:60px;height:60px;object-fit:cover" 
                 class="me-2"/>
            <div>
                <div style="font-size:14px"><strong>${product.name}</strong></div>
                <div class="text-danger fw-bold" style="font-size:13px">
                    ${product.price.toLocaleString("vi-VN")} VNĐ
                </div>
            </div>
        </div>
    </div>`;
                chatBox.appendChild(productCard);
            }
        }
        if (isInitialLoad) {
            // lần đầu load => cuộn xuống cuối
            chatBox.scrollTop = chatBox.scrollHeight;
        } else {
            // load thêm cũ => giữ nguyên vị trí
            const newScrollHeight = chatBox.scrollHeight;
            chatBox.scrollTop += newScrollHeight - oldScrollHeight;
        }
    },
    openChat: async function (BuyerId, productId) {
        const chatWindow = document.getElementById("chatWindow");
        chatWindow.style.display = "flex";
        const conRes = await fetch(`/Chat/GetConversation?buyerId=${BuyerId}`);
        const conData = await conRes.json();
        if (!conData.success) {
            alert(conData.message || "Không tìm thấy cuộc trò chuyện");
            return;
        }
        const conversationId = conData.conversationId;
        check = false;
        this.loadMessages(conversationId,null, productId);
        
    },
    replyMessage: function (messageId, content, senderName) {
        debugger;
        replyingToMessageId = messageId;
        var name = "chính mình";
        if (senderName.toLowerCase() === "admin")
        name = "Admin";
        const container = document.getElementById("reply-box-container");
        container.innerHTML = `
        <div class="reply-box d-flex justify-content-between align-items-center p-2 border-start border-primary bg-light rounded">
            <div class="me-2">
                <div class="fw-bold small">Đang trả lời '${name}'</div>
                <div class="text-truncate small text-muted" style="max-width:200px;">${content}</div>
            </div>
            <button type="button" class="btn btn-sm btn-link text-danger p-0" onclick="chat.cancelReply()">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>
    `;
    },

    cancelReply: function () {
        replyingToMessageId = null;
        document.getElementById("reply-box-container").innerHTML = "";
    },

};

buyerConnection.on("ReceiveMessage", async function (msg) {
    console.log("ReceiveMessage fired:", msg);

    if (msg.conversationId !== conversationId) return;
    chatBox = document.getElementById("chat-messages");
    const chatWindow = document.getElementById("chatWindow");
    const computedStyle = window.getComputedStyle(chatWindow);
    if (!chatWindow || computedStyle.display === "none") {
        // Chat chưa mở → mở chat
        chat.openChat(buyerId, msg.productId);
    }
    // đảm bảo tin nhắn gốc có sẵn
    let replyHTML = "";

    if (msg.replyToMessageId) {
        let repliedMsg = messages.find(m => m.messageId === msg.replyToMessageId);
        if (!repliedMsg) {
            debugger;
            const res = await fetch(`/Chat/GetMessageById?messageId=${msg.replyToMessageId}`);
            const data = await res.json();
            repliedMsg = data.message;
            messages.push(repliedMsg);
        }
        let replyText = "";
        const repliedSender = repliedMsg?.senderName?.toLowerCase(); // ✅ unify
        const sender = msg.senderName?.toLowerCase();

        if (sender === "admin") {
            // Admin đang trả lời
            if (repliedSender === "admin") {
                replyText = "Admin đã trả lời chính mình";
            } else {
                replyText = "Admin đã trả lời bạn";
            }
        } else {
            console.log(repliedMsg);
            console.log(repliedMsg.senderName);
            // Người dùng đang trả lời
            if (repliedSender === "admin") {
                replyText = "Bạn đã trả lời Admin";
            } else {
                replyText = "Bạn đã trả lời chính mình";
            }
        }

        replyHTML = `
        <div class="message-reply">
            <span class="reply-author">${replyText}</span>
            <div class="reply-content">${repliedMsg.content || repliedMsg.message?.content || ""}</div>
        </div>
    `;
    }
    // push tin nhắn mới vào mảng
    // Thay thế block này
    if (!messages.find(m => m.messageId === msg.messageId)) {
        messages.push(msg);
    }

    // render bubble
    const bubble = document.createElement("div");
    bubble.className = msg.senderRole === "admin" ? "chat-message admin" : "chat-message buyer";
    bubble.dataset.messageId = msg.messageId;
    const date = new Date(msg.createdAt);
    const timeString = date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });

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
                    <li><a class="dropdown-item" href="#" onclick="chat.replyMessage('${msg.messageId}', '${msg.content}', '${msg.senderName}')">Trả lời</a></li>
                </ul>
            </div>
        </div>
    `;

    chatBox.appendChild(bubble);
    chatBox.scrollTop = chatBox.scrollHeight;
    if (msg.replyToMessageId) {
        chat.cancelReply();
    }
});

if (buyerId) {
    chat.initChat(buyerId);
}

